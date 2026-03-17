using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        const string UnknownCustomerName  = "Unknown";
        const string FallbackChannelType  = "Direct";
        const string FallbackCustomerType = "Retail";

        // ── All-orders endpoint (no customer filter required) ─────────────────
        app.MapGet("/orders", async (CustomerDbContext db,
            Guid? customerId = null, string? status = null, string? channel = null, DateOnly? weekOf = null) =>
        {
            var q = db.Orders.Include(i => i.Lines).AsQueryable();

            if (customerId.HasValue)
                q = q.Where(i => i.CustomerId == customerId.Value);

            if (Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var s))
                q = q.Where(i => i.Status == s);

            if (Enum.TryParse<OrderChannel>(channel, ignoreCase: true, out var ch))
                q = q.Where(i => i.Channel == ch);

            if (weekOf.HasValue)
            {
                var weekStart = weekOf.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                q = q.Where(i => i.WeekOf >= weekStart && i.WeekOf < weekStart.AddDays(7));
            }

            var items = await q.OrderByDescending(i => i.WeekOf).ToListAsync();
            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();

            var customerIds = items.Select(i => i.CustomerId).Distinct().ToList();
            var customers = await db.Customers
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => new { c.Id, c.DisplayName, c.ChannelType, c.Type })
                .ToListAsync();
            var customerMap = customers.ToDictionary(c => c.Id);

            var result = items.Select(i =>
            {
                var cust = customerMap.GetValueOrDefault(i.CustomerId);
                var (qty, amount) = PriceResolutionService.ComputeOrderTotals(i.Lines, boxConfigs);
                return new AllOrdersSummaryDto(
                    i.Id, i.StandingOrderId, i.CustomerId,
                    cust?.DisplayName ?? UnknownCustomerName,
                    cust?.ChannelType.ToString() ?? FallbackChannelType,
                    cust?.Type.ToString() ?? FallbackCustomerType,
                    i.Channel.ToString(), i.Status.ToString(), i.WeekOf, i.IsSample,
                    qty, amount);
            }).ToList();

            return Results.Ok(result);
        }).WithTags("Orders").Produces<IEnumerable<AllOrdersSummaryDto>>();

        var g = app.MapGroup("/customers/{customerId:guid}/orders").WithTags("Orders");

        g.MapGet("/", async (Guid customerId, CustomerDbContext db,
            string? status = null, string? channel = null, int page = 1, int pageSize = 50) =>
        {
            var q = db.Orders
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customerId);

            if (Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var s))
                q = q.Where(i => i.Status == s);

            if (Enum.TryParse<OrderChannel>(channel, ignoreCase: true, out var ch))
                q = q.Where(i => i.Channel == ch);

            var items = await q.OrderByDescending(i => i.WeekOf)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(items.Select(i => ToDto(i, boxConfigs)));
        }).Produces<IEnumerable<OrderDto>>();

        g.MapGet("/{orderId:guid}", async (Guid customerId, Guid orderId, CustomerDbContext db) =>
        {
            var order = await db.Orders.Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == orderId && i.CustomerId == customerId);
            if (order is null) return Results.NotFound();
            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(ToDto(order, boxConfigs));
        }).Produces<OrderDto>();

        // ── Unified order creation (both Insulated and FedEx) ─────────────────
        /// <remarks>
        /// For Insulated: finds or auto-creates a StandingOrder, then creates the Order instance.
        /// For FedEx: creates the Order directly with StandingOrderId = null (ad-hoc, no subscription).
        /// </remarks>
        g.MapPost("/", async (Guid customerId, CreateOrderRequest req,
            CustomerDbContext db, HttpContext ctx) =>
        {
            if (!await db.Customers.AnyAsync(c => c.Id == customerId))
                return Results.NotFound();

            if (!req.Lines.Any())
                return Results.BadRequest("At least one line is required.");

            if (req.ContactId.HasValue &&
                !await db.CustomerContacts.AnyAsync(c => c.Id == req.ContactId && c.CustomerId == customerId))
                return Results.BadRequest("ContactId does not belong to this customer.");

            var weekOf = DateTime.SpecifyKind(
                SeasonYearService.MondayOf(req.WeekOf).Date, DateTimeKind.Utc);
            var userId = ctx.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";

            Guid? standingOrderId = null;

            if (req.Channel == OrderChannel.Insulated)
            {
                // Find or auto-create the StandingOrder for this (customer, channel) pair
                var standingOrder = await db.StandingOrders
                    .FirstOrDefaultAsync(s => s.CustomerId == customerId
                                           && s.Channel == OrderChannel.Insulated
                                           && s.Status != StandingOrderStatus.Stopped);

                if (standingOrder is null)
                {
                    var seasonYear = SeasonYearService.CurrentSeasonYear(weekOf);
                    standingOrder = new StandingOrder
                    {
                        Id         = Guid.NewGuid(),
                        CustomerId = customerId,
                        Channel    = OrderChannel.Insulated,
                        ContactId  = req.ContactId,
                        Frequency  = OrderFrequency.Weekly,
                        IsSample   = req.IsSample,
                        Status     = StandingOrderStatus.Active,
                        SeasonYear = seasonYear,
                        StartWeek  = weekOf,
                        CreatedAt  = DateTime.UtcNow,
                        CreatedBy  = userId,
                        Lines      = req.Lines
                            .Where(l => l.BoxSize.HasValue)
                            .Select(l => new StandingOrderLine
                            {
                                Id              = Guid.NewGuid(),
                                StandingOrderId = standingOrder!.Id,
                                BoxSize         = l.BoxSize!.Value,
                                Qty             = l.Qty
                            }).ToList()
                    };
                    db.StandingOrders.Add(standingOrder);
                }

                // Idempotency: reject duplicate order for the same standing order + week
                var duplicate = await db.Orders.AnyAsync(o =>
                    o.StandingOrderId == standingOrder.Id && o.WeekOf.Date == weekOf.Date);
                if (duplicate)
                    return Results.Conflict("An order already exists for this standing order and week.");

                standingOrderId = standingOrder.Id;
            }

            ICollection<OrderLine> lines;
            if (req.Channel == OrderChannel.Insulated)
            {
                var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
                var overrides  = await db.CustomerPricings.Where(p => p.CustomerId == customerId).ToListAsync();
                lines = req.Lines
                    .Where(l => l.BoxSize.HasValue)
                    .Select(l => new OrderLine
                    {
                        Id                  = Guid.NewGuid(),
                        BoxSize             = l.BoxSize,
                        Qty                 = l.Qty,
                        EffectivePricePerLb = PriceResolutionService.ResolveEffectivePricePerLb(
                                                 l.BoxSize!.Value, l.Qty, overrides)
                    }).ToList();
            }
            else
            {
                var tierConfigs = await db.FedExTierConfigs.ToListAsync();
                lines = req.Lines
                    .Where(l => l.FedExTierSize.HasValue)
                    .Select(l =>
                    {
                        var config  = tierConfigs.First(t => t.TierSize == l.FedExTierSize!.Value);
                        var totalOz = config.WeightOz * l.Qty;
                        return new OrderLine
                        {
                            Id              = Guid.NewGuid(),
                            FedExTierSize   = l.FedExTierSize,
                            FedExFixedPrice = config.FixedPrice,
                            Qty             = l.Qty,
                            PackagingType   = PriceResolutionService.DerivePackagingType(totalOz)
                        };
                    }).ToList();
            }

            var order = new Order
            {
                Id              = Guid.NewGuid(),
                StandingOrderId = standingOrderId,
                CustomerId      = customerId,
                ContactId       = req.ContactId,
                Channel         = req.Channel,
                Status          = OrderStatus.Pending,
                WeekOf          = weekOf,
                IsSample        = req.IsSample,
                CreatedAt       = DateTime.UtcNow,
                CreatedBy       = userId,
                Lines           = lines
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var allBoxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Created($"/customers/{customerId}/orders/{order.Id}",
                ToDto(order, allBoxConfigs));
        }).Produces<OrderDto>(StatusCodes.Status201Created);

        g.MapPost("/{orderId:guid}/harvest", async (Guid customerId, Guid orderId,
            CustomerDbContext db, HttpContext ctx) =>
            await TransitionAsync(customerId, orderId, OrderStatus.Pending,
                OrderStatus.Harvested, db, ctx));

        g.MapPost("/{orderId:guid}/inspect", async (Guid customerId, Guid orderId,
            InspectOrderRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var inspectDay = req.InspectionDate.DayOfWeek;
            if (inspectDay != DayOfWeek.Friday && inspectDay != DayOfWeek.Monday)
                return Results.UnprocessableEntity("USDA inspection only occurs on Fridays and Mondays.");

            return await TransitionAsync(customerId, orderId, OrderStatus.Harvested,
                OrderStatus.Inspected, db, ctx);
        });

        g.MapPost("/{orderId:guid}/ship", async (Guid customerId, Guid orderId,
            CustomerDbContext db, HttpContext ctx) =>
        {
            var order = await db.Orders.Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == orderId && i.CustomerId == customerId);
            if (order is null) return Results.NotFound();
            if (order.Status != OrderStatus.Inspected)
                return Results.UnprocessableEntity($"Cannot ship from status {order.Status}.");

            order.Status     = OrderStatus.Shipped;
            order.ShipDate   = DateTime.UtcNow;
            order.ModifiedAt = DateTime.UtcNow;
            order.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();

            if (!order.IsSample)
                await CreateInvoiceAsync(order, db);

            await db.SaveChangesAsync();
            var shipBoxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(ToDto(order, shipBoxConfigs));
        });

        g.MapPatch("/{orderId:guid}", async (Guid customerId, Guid orderId,
            UpdateOrderRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var order = await db.Orders
                .FirstOrDefaultAsync(i => i.Id == orderId && i.CustomerId == customerId);
            if (order is null) return Results.NotFound();

            if (order.Status != OrderStatus.Pending)
                return Results.UnprocessableEntity("Only Pending orders can be edited.");

            if (req.ContactId.HasValue &&
                !await db.CustomerContacts.AnyAsync(c => c.Id == req.ContactId && c.CustomerId == customerId))
                return Results.BadRequest("ContactId does not belong to this customer.");

            order.ContactId  = req.ContactId;
            order.IsSample   = req.IsSample;
            order.ModifiedAt = DateTime.UtcNow;
            order.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();

            await db.SaveChangesAsync();
            await db.Entry(order).Collection(x => x.Lines).LoadAsync();
            var patchBoxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(ToDto(order, patchBoxConfigs));
        }).Produces<OrderDto>();

        g.MapPost("/{orderId:guid}/cancel", async (Guid customerId, Guid orderId,
            CustomerDbContext db, HttpContext ctx) =>
        {
            var order = await db.Orders
                .FirstOrDefaultAsync(i => i.Id == orderId && i.CustomerId == customerId);
            if (order is null) return Results.NotFound();

            if (order.Status != OrderStatus.Pending)
                return Results.UnprocessableEntity("Only Pending orders can be cancelled.");

            // Check Friday cutoff for standing orders
            var cutoff = SeasonYearService.MondayOf(order.WeekOf).AddDays(-3);
            if (DateTime.UtcNow.Date > cutoff.Date)
                return Results.UnprocessableEntity("Friday cutoff has passed; order will be shipped.");

            order.Status     = OrderStatus.Cancelled;
            order.ModifiedAt = DateTime.UtcNow;
            order.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        return app;
    }

    private static async Task<IResult> TransitionAsync(
        Guid customerId, Guid orderId,
        OrderStatus from, OrderStatus to,
        CustomerDbContext db, HttpContext ctx)
    {
        var order = await db.Orders.Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == orderId && i.CustomerId == customerId);
        if (order is null) return Results.NotFound();
        if (order.Status != from)
            return Results.UnprocessableEntity($"Expected status {from}, found {order.Status}.");

        order.Status     = to;
        order.ModifiedAt = DateTime.UtcNow;
        order.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
        await db.SaveChangesAsync();
        var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
        return Results.Ok(ToDto(order, boxConfigs));
    }

    /// <summary>Creates an invoice for a shipped order. Does NOT create StandingOrders — that is done at order creation time.</summary>
    public static async Task CreateInvoiceAsync(Order order, CustomerDbContext db)
    {
        var customer    = await db.Customers.FindAsync(order.CustomerId);
        var customerKey = customer?.CustomerKey ?? $"UNK-{order.CustomerId.ToString()[..4]}";
        var seasonYear  = SeasonYearService.CurrentSeasonYear(order.ShipDate ?? DateTime.UtcNow);

        var seekNum = await db.Invoices
            .Where(inv => inv.CustomerId == order.CustomerId
                       && inv.SeasonYear  == seasonYear
                       && inv.Channel     == order.Channel)
            .Select(inv => (int?)inv.SeekNum)
            .MaxAsync() ?? 0;
        seekNum++;

        var label = InvoiceLabelService.BuildLabel(customerKey, order.Channel, seasonYear, seekNum);

        db.Invoices.Add(new Invoice
        {
            Id         = Guid.NewGuid(),
            OrderId    = order.Id,
            CustomerId = order.CustomerId,
            Channel    = order.Channel,
            SeasonYear = seasonYear,
            SeekNum    = seekNum,
            Label      = label,
            CreatedAt  = DateTime.UtcNow
        });
    }

    internal static OrderDto ToDto(Order o, IEnumerable<InsulatedBoxConfig> boxConfigs)
    {
        var (totalQty, totalAmount) = PriceResolutionService.ComputeOrderTotals(o.Lines, boxConfigs);
        return new(
            o.Id, o.StandingOrderId, o.CustomerId, o.ContactId,
            o.Channel, o.Status, o.WeekOf, o.ShipDate, o.IsSample,
            o.Lines.Select(l => new OrderLineDto(
                l.Id, l.BoxSize, l.Qty, l.EffectivePricePerLb,
                l.FedExTierSize, l.FedExFixedPrice, l.PackagingType)).ToList(),
            o.CreatedAt, totalQty, totalAmount);
    }
}
