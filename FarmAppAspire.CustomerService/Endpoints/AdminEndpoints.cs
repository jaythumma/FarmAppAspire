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

        // ── Manual instance generation trigger ────────────────────────────────
        g.MapPost("/generate-instances", async (GenerateInstancesRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var userId     = ctx.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
            var weekOf     = SeasonYearService.MondayOf(req.ForWeek);
            var seasonYear = SeasonYearService.CurrentSeasonYear(weekOf);

            var activeOrders = await db.StandingOrders
                .Include(s => s.Lines)
                .Include(s => s.Skips)
                .Where(s => s.Status == StandingOrderStatus.Active
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

            var created  = 0;
            var skipped  = 0;
            var createdInstances = new List<(OrderInstance Instance, OrderFrequency Frequency)>();

            foreach (var so in activeOrders)
            {
                // Idempotency: skip if instance already exists
                if (await db.OrderInstances.AnyAsync(i =>
                        i.StandingOrderId == so.Id && i.WeekOf.Date == weekOf.Date))
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

                var instance = new OrderInstance
                {
                    Id              = Guid.NewGuid(),
                    StandingOrderId = so.Id,
                    CustomerId      = so.CustomerId,
                    ContactId       = so.ContactId,
                    Channel         = OrderChannel.Insulated,
                    Status          = OrderInstanceStatus.Pending,
                    WeekOf          = weekOf,
                    IsSample        = so.IsSample,
                    CreatedAt       = DateTime.UtcNow,
                    CreatedBy       = userId,
                    Lines           = so.Lines.Select(l =>
                    {
                        var pricePerLb = PriceResolutionService.ResolveEffectivePricePerLb(l.BoxSize, l.Qty, pricingOverrides);
                        return new OrderInstanceLine
                        {
                            Id                  = Guid.NewGuid(),
                            BoxSize             = l.BoxSize,
                            Qty                 = l.Qty,
                            EffectivePricePerLb = pricePerLb
                        };
                    }).ToList()
                };

                db.OrderInstances.Add(instance);
                createdInstances.Add((instance, so.Frequency));

                // OnRequest: generate then auto-pause
                if (so.Frequency == OrderFrequency.OnRequest)
                    so.Status = StandingOrderStatus.Paused;

                created++;
            }

            await db.SaveChangesAsync();

            var summaries = createdInstances.Select(t =>
            {
                var inst  = t.Instance;
                var cust  = customerMap.GetValueOrDefault(inst.CustomerId);
                var (qty, amount) = PriceResolutionService.ComputeOrderInstanceTotals(inst.Lines, boxConfigs);
                return new GeneratedInstanceSummary(
                    inst.Id,
                    inst.CustomerId,
                    cust?.DisplayName ?? "Unknown",
                    cust?.CustomerKey,
                    inst.Channel.ToString(),
                    inst.WeekOf,
                    inst.IsSample,
                    qty,
                    amount);
            }).OrderBy(s => s.CustomerDisplayName).ToList();

            return Results.Ok(new GenerateInstancesResponse(created, skipped, weekOf, summaries));
        }).Produces<GenerateInstancesResponse>();

        // ── Browse instances for a given week ─────────────────────────────────
        g.MapGet("/instances", async (CustomerDbContext db, DateTime? weekOf = null) =>
        {
            var week = SeasonYearService.MondayOf(weekOf ?? DateTime.UtcNow);

            try
            {
                var instances = await db.OrderInstances
                    .Include(i => i.Lines)
                    .Where(i => i.WeekOf.Date == week.Date)
                    .OrderBy(i => i.CustomerId)
                    .ToListAsync();

                var boxConfigs  = await db.InsulatedBoxConfigs.ToListAsync();
                var customerIds = instances.Select(i => i.CustomerId).Distinct().ToList();
                var customers   = await db.Customers
                    .Where(c => customerIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.DisplayName, c.CustomerKey })
                    .ToListAsync();
                var customerMap = customers.ToDictionary(c => c.Id);

                var result = instances.Select(i =>
                {
                    var cust  = customerMap.GetValueOrDefault(i.CustomerId);
                    var (qty, amount) = PriceResolutionService.ComputeOrderInstanceTotals(i.Lines, boxConfigs);
                    return new WeekInstanceSummary(
                        i.Id,
                        i.CustomerId,
                        cust?.DisplayName ?? "Unknown",
                        cust?.CustomerKey,
                        i.Channel.ToString(),
                        i.Status.ToString(),
                        i.WeekOf,
                        i.IsSample,
                        qty,
                        amount);
                }).ToList();

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }).Produces<IEnumerable<WeekInstanceSummary>>();


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
