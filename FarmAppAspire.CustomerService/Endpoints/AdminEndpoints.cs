using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/admin").WithTags("Admin");

        // ── Manual order generation trigger ───────────────────────────────────
        /// <remarks>Only generates orders for Insulated standing orders. FedEx standing orders are on-demand and excluded.</remarks>
        g.MapPost("/generate-orders", async (GenerateOrdersRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var userId     = ctx.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
            var weekOf     = DateTime.SpecifyKind(SeasonYearService.MondayOf(req.ForWeek).Date, DateTimeKind.Utc);
            var seasonYear = SeasonYearService.CurrentSeasonYear(weekOf);

            var activeOrders = await db.StandingOrders
                .Include(s => s.Lines)
                .Include(s => s.Skips)
                .Where(s => s.Channel == OrderChannel.Insulated  // FedEx SOs are always on-demand; exclude from bulk generate
                         && s.Status == StandingOrderStatus.Active
                         && s.Frequency != OrderFrequency.Stopped
                         && s.SeasonYear <= seasonYear)
                .ToListAsync();

            var boxConfigs      = await db.InsulatedBoxConfigs.ToListAsync();
            var customerIds     = activeOrders.Select(s => s.CustomerId).Distinct().ToList();
            var customers       = await db.Customers
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => new { c.Id, c.DisplayName, c.CustomerKey })
                .ToListAsync();
            var customerMap     = customers.ToDictionary(c => c.Id);

            var created       = 0;
            var skipped       = 0;
            var createdOrders = new List<(Order Order, OrderFrequency Frequency)>();

            foreach (var so in activeOrders)
            {
                // Idempotency: skip if order already exists for this week
                if (await db.Orders.AnyAsync(i =>
                        i.StandingOrderId == so.Id && i.WeekOf >= weekOf && i.WeekOf < weekOf.AddDays(1)))
                {
                    skipped++;
                    continue;
                }

                var skippedDates = so.Skips.Select(s => s.WeekOf);
                var shouldGenerate = StandingOrderScheduleService.ShouldGenerateForWeek(
                    so.Frequency, so.StartWeek, skippedDates,
                    lastShippedWeek: null, weekOf, so.MonthlyWeek);

                if (!shouldGenerate) { skipped++; continue; }

                var pricingOverrides = await db.CustomerPricings
                    .Where(p => p.CustomerId == so.CustomerId).ToListAsync();

                var order = new Order
                {
                    Id              = Guid.NewGuid(),
                    StandingOrderId = so.Id,
                    CustomerId      = so.CustomerId,
                    ContactId       = so.ContactId,
                    Channel         = OrderChannel.Insulated,
                    Status          = OrderStatus.Pending,
                    WeekOf          = weekOf,
                    IsSample        = so.IsSample,
                    CreatedAt       = DateTime.UtcNow,
                    CreatedBy       = userId,
                    Lines           = so.Lines.Select(l =>
                    {
                        var pricePerLb = PriceResolutionService.ResolveEffectivePricePerLb(l.BoxSize, l.Qty, pricingOverrides);
                        return new OrderLine
                        {
                            Id                  = Guid.NewGuid(),
                            BoxSize             = l.BoxSize,
                            Qty                 = l.Qty,
                            EffectivePricePerLb = pricePerLb
                        };
                    }).ToList()
                };

                db.Orders.Add(order);
                createdOrders.Add((order, so.Frequency));

                // OnRequest: generate then auto-pause
                if (so.Frequency == OrderFrequency.OnRequest)
                    so.Status = StandingOrderStatus.Paused;

                created++;
            }

            await db.SaveChangesAsync();

            var summaries = createdOrders.Select(t =>
            {
                var o    = t.Order;
                var cust = customerMap.GetValueOrDefault(o.CustomerId);
                var (qty, amount) = PriceResolutionService.ComputeOrderTotals(o.Lines, boxConfigs);
                return new GeneratedOrderSummary(
                    o.Id,
                    o.CustomerId,
                    cust?.DisplayName ?? "Unknown",
                    cust?.CustomerKey,
                    o.Channel.ToString(),
                    o.WeekOf,
                    o.IsSample,
                    qty,
                    amount);
            }).OrderBy(s => s.CustomerDisplayName).ToList();

            return Results.Ok(new GenerateOrdersResponse(created, skipped, weekOf, summaries));
        }).Produces<GenerateOrdersResponse>();

        // ── Browse orders for a given week ─────────────────────────────────────
        g.MapGet("/orders", async (CustomerDbContext db, DateTime? weekOf = null) =>
        {
            var weekStart = DateTime.SpecifyKind(SeasonYearService.MondayOf(weekOf ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
            var weekEnd   = weekStart.AddDays(1);

            try
            {
                var orders = await db.Orders
                    .Include(i => i.Lines)
                    .Where(i => i.WeekOf >= weekStart && i.WeekOf < weekEnd)
                    .OrderBy(i => i.CustomerId)
                    .ToListAsync();

                var boxConfigs  = await db.InsulatedBoxConfigs.ToListAsync();
                var customerIds = orders.Select(i => i.CustomerId).Distinct().ToList();
                var customers   = await db.Customers
                    .Where(c => customerIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.DisplayName, c.CustomerKey })
                    .ToListAsync();
                var customerMap = customers.ToDictionary(c => c.Id);

                var result = orders.Select(o =>
                {
                    var cust  = customerMap.GetValueOrDefault(o.CustomerId);
                    var (qty, amount) = PriceResolutionService.ComputeOrderTotals(o.Lines, boxConfigs);
                    return new WeekOrderSummary(
                        o.Id,
                        o.CustomerId,
                        cust?.DisplayName ?? "Unknown",
                        cust?.CustomerKey,
                        o.Channel.ToString(),
                        o.Status.ToString(),
                        o.WeekOf,
                        o.IsSample,
                        qty,
                        amount);
                }).OrderBy(s => s.CustomerDisplayName).ToList();

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }).Produces<IEnumerable<WeekOrderSummary>>();


        // ── Season management ─────────────────────────────────────────────────
        g.MapGet("/season/current", () =>
        {
            var now        = DateTime.UtcNow;
            var seasonYear = SeasonYearService.CurrentSeasonYear(now);
            return Results.Ok(new
            {
                SeasonYear = seasonYear,
                Start      = SeasonYearService.FirstMondayOfJune(seasonYear),
                End        = SeasonYearService.LastMondayOfMayFollowing(seasonYear)
            });
        });

        g.MapPost("/season/restart", async (SeasonRestartRequest req, CustomerDbContext db) =>
        {
            var activeOrders = await db.StandingOrders
                .Where(s => s.Status == StandingOrderStatus.Active
                         || s.Status == StandingOrderStatus.Paused)
                .ToListAsync();

            foreach (var so in activeOrders)
                so.SeasonYear = req.TargetSeasonYear;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                TargetSeasonYear = req.TargetSeasonYear,
                UpdatedOrders    = activeOrders.Count
            });
        });

        return app;
    }
}
