using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class OrderInstanceEndpoints
{
    public static IEndpointRouteBuilder MapOrderInstanceEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/customers/{customerId:guid}/order-instances").WithTags("OrderInstances");

        g.MapGet("/", async (Guid customerId, CustomerDbContext db,
            string? status = null, int page = 1, int pageSize = 50) =>
        {
            var q = db.OrderInstances
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customerId);

            if (Enum.TryParse<OrderInstanceStatus>(status, ignoreCase: true, out var s))
                q = q.Where(i => i.Status == s);

            var items = await q.OrderByDescending(i => i.WeekOf)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(items.Select(i => ToDto(i, boxConfigs)));
        }).Produces<IEnumerable<OrderInstanceDto>>();

        g.MapGet("/{instanceId:guid}", async (Guid customerId, Guid instanceId, CustomerDbContext db) =>
        {
            var i = await db.OrderInstances.Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == instanceId && i.CustomerId == customerId);
            if (i is null) return Results.NotFound();
            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(ToDto(i, boxConfigs));
        }).Produces<OrderInstanceDto>();

        g.MapPost("/{instanceId:guid}/harvest", async (Guid customerId, Guid instanceId,
            CustomerDbContext db, HttpContext ctx) =>
            await TransitionAsync(customerId, instanceId, OrderInstanceStatus.Pending,
                OrderInstanceStatus.Harvested, db, ctx));

        g.MapPost("/{instanceId:guid}/inspect", async (Guid customerId, Guid instanceId,
            InspectOrderRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var inspectDay = req.InspectionDate.DayOfWeek;
            if (inspectDay != DayOfWeek.Friday && inspectDay != DayOfWeek.Monday)
                return Results.UnprocessableEntity("USDA inspection only occurs on Fridays and Mondays.");

            return await TransitionAsync(customerId, instanceId, OrderInstanceStatus.Harvested,
                OrderInstanceStatus.Inspected, db, ctx);
        });

        g.MapPost("/{instanceId:guid}/ship", async (Guid customerId, Guid instanceId,
            CustomerDbContext db, HttpContext ctx) =>
        {
            var instance = await db.OrderInstances.Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == instanceId && i.CustomerId == customerId);
            if (instance is null) return Results.NotFound();
            if (instance.Status != OrderInstanceStatus.Inspected)
                return Results.UnprocessableEntity($"Cannot ship from status {instance.Status}.");

            instance.Status     = OrderInstanceStatus.Shipped;
            instance.ShipDate   = DateTime.UtcNow;
            instance.ModifiedAt = DateTime.UtcNow;
            instance.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();

            // Generate invoice if not a sample
            if (!instance.IsSample)
                await CreateInvoiceAsync(instance, db);

            await db.SaveChangesAsync();
            var shipBoxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(ToDto(instance, shipBoxConfigs));
        });

        g.MapPatch("/{instanceId:guid}", async (Guid customerId, Guid instanceId,
            UpdateOrderInstanceRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var instance = await db.OrderInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId && i.CustomerId == customerId);
            if (instance is null) return Results.NotFound();

            if (instance.Status != OrderInstanceStatus.Pending)
                return Results.UnprocessableEntity("Only Pending orders can be edited.");

            if (req.ContactId.HasValue &&
                !await db.CustomerContacts.AnyAsync(c => c.Id == req.ContactId && c.CustomerId == customerId))
                return Results.BadRequest("ContactId does not belong to this customer.");

            instance.ContactId  = req.ContactId;
            instance.IsSample   = req.IsSample;
            instance.ModifiedAt = DateTime.UtcNow;
            instance.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();

            await db.SaveChangesAsync();
            await db.Entry(instance).Collection(x => x.Lines).LoadAsync();
            var patchBoxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            return Results.Ok(ToDto(instance, patchBoxConfigs));
        }).Produces<OrderInstanceDto>();

        g.MapPost("/{instanceId:guid}/cancel", async (Guid customerId, Guid instanceId,
            CustomerDbContext db, HttpContext ctx) =>
        {
            var instance = await db.OrderInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId && i.CustomerId == customerId);
            if (instance is null) return Results.NotFound();

            if (instance.Status != OrderInstanceStatus.Pending)
                return Results.UnprocessableEntity("Only Pending instances can be cancelled.");

            // For standing-order instances: check Friday cutoff
            if (instance.StandingOrderId.HasValue)
            {
                var cutoff = SeasonYearService.MondayOf(instance.WeekOf).AddDays(-3);
                if (DateTime.UtcNow.Date > cutoff.Date)
                    return Results.UnprocessableEntity("Friday cutoff has passed; order will be shipped.");
            }

            instance.Status     = OrderInstanceStatus.Cancelled;
            instance.ModifiedAt = DateTime.UtcNow;
            instance.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        return app;
    }

    private static async Task<IResult> TransitionAsync(
        Guid customerId, Guid instanceId,
        OrderInstanceStatus from, OrderInstanceStatus to,
        CustomerDbContext db, HttpContext ctx)
    {
        var instance = await db.OrderInstances.Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CustomerId == customerId);
        if (instance is null) return Results.NotFound();
        if (instance.Status != from)
            return Results.UnprocessableEntity($"Expected status {from}, found {instance.Status}.");

        instance.Status     = to;
        instance.ModifiedAt = DateTime.UtcNow;
        instance.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
        await db.SaveChangesAsync();
        var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
        return Results.Ok(ToDto(instance, boxConfigs));
    }

    public static async Task CreateInvoiceAsync(OrderInstance instance, CustomerDbContext db)
    {
        var customer = await db.Customers.FindAsync(instance.CustomerId);
        var customerKey = customer?.CustomerKey ?? $"UNK-{instance.CustomerId.ToString()[..4]}";
        var seasonYear  = SeasonYearService.CurrentSeasonYear(instance.ShipDate ?? DateTime.UtcNow);

        // Auto-create a standing order for first-time invoiced customers
        var hasStandingOrder = await db.StandingOrders.AnyAsync(s => s.CustomerId == instance.CustomerId);
        if (!hasStandingOrder)
        {
            var startWeek = SeasonYearService.MondayOf(instance.ShipDate ?? DateTime.UtcNow);
            db.StandingOrders.Add(new StandingOrder
            {
                Id         = Guid.NewGuid(),
                CustomerId = instance.CustomerId,
                ContactId  = instance.ContactId,
                Frequency  = OrderFrequency.Weekly,
                IsSample   = false,
                Status     = StandingOrderStatus.Active,
                SeasonYear = seasonYear,
                StartWeek  = startWeek,
                CreatedAt  = DateTime.UtcNow,
                CreatedBy  = instance.CreatedBy
            });
        }

        // Atomic SeekNum assignment: use DB serialisation via EF transaction
        var seekNum = await db.Invoices
            .Where(inv => inv.CustomerId == instance.CustomerId
                       && inv.SeasonYear  == seasonYear
                       && inv.Channel     == instance.Channel)
            .Select(inv => (int?)inv.SeekNum)
            .MaxAsync() ?? 0;
        seekNum++;

        var label = InvoiceLabelService.BuildLabel(customerKey, instance.Channel, seasonYear, seekNum);

        db.Invoices.Add(new Invoice
        {
            Id              = Guid.NewGuid(),
            OrderInstanceId = instance.Id,
            CustomerId      = instance.CustomerId,
            Channel         = instance.Channel,
            SeasonYear      = seasonYear,
            SeekNum         = seekNum,
            Label           = label,
            CreatedAt       = DateTime.UtcNow
        });
    }

    internal static OrderInstanceDto ToDto(OrderInstance i, IEnumerable<InsulatedBoxConfig> boxConfigs)
    {
        var (totalQty, totalAmount) = PriceResolutionService.ComputeOrderInstanceTotals(i.Lines, boxConfigs);
        return new(
            i.Id, i.StandingOrderId, i.CustomerId, i.ContactId,
            i.Channel, i.Status, i.WeekOf, i.ShipDate, i.IsSample,
            i.Lines.Select(l => new OrderInstanceLineDto(
                l.Id, l.BoxSize, l.Qty, l.EffectivePricePerLb,
                l.FedExTierSize, l.FedExFixedPrice, l.PackagingType)).ToList(),
            i.CreatedAt, totalQty, totalAmount);
    }
}
