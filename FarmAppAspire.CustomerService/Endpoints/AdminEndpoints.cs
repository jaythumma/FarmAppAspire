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

            var created = 0;
            foreach (var so in activeOrders)
            {
                // Idempotency: skip if instance already exists
                if (await db.OrderInstances.AnyAsync(i =>
                        i.StandingOrderId == so.Id && i.WeekOf.Date == weekOf.Date))
                    continue;

                var skippedDates = so.Skips.Select(s => s.WeekOf);
                var shouldGenerate = StandingOrderScheduleService.ShouldGenerateForWeek(
                    so.Frequency, so.StartWeek, skippedDates,
                    lastShippedWeek: null, weekOf, so.MonthlyWeek);

                if (!shouldGenerate) continue;

                var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
                var pricingOverrides = await db.CustomerPricings
                    .Where(p => p.CustomerId == so.CustomerId).ToListAsync();

                var instance = new OrderInstance
                {
                    Id             = Guid.NewGuid(),
                    StandingOrderId = so.Id,
                    CustomerId     = so.CustomerId,
                    ContactId      = so.ContactId,
                    Channel        = OrderChannel.Insulated,
                    Status         = OrderInstanceStatus.Pending,
                    WeekOf         = weekOf,
                    IsSample       = so.IsSample,
                    CreatedAt      = DateTime.UtcNow,
                    CreatedBy      = userId,
                    Lines          = so.Lines.Select(l =>
                    {
                        var config    = boxConfigs.First(b => b.Size == l.BoxSize);
                        var pricePerLb = PriceResolutionService.ResolveEffectivePricePerLb(l.BoxSize, l.Qty, pricingOverrides);
                        return new OrderInstanceLine
                        {
                            Id                = Guid.NewGuid(),
                            BoxSize           = l.BoxSize,
                            Qty               = l.Qty,
                            EffectivePricePerLb = pricePerLb
                        };
                    }).ToList()
                };

                db.OrderInstances.Add(instance);

                // OnRequest: generate then auto-pause
                if (so.Frequency == OrderFrequency.OnRequest)
                    so.Status = StandingOrderStatus.Paused;

                created++;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { GeneratedCount = created, WeekOf = weekOf });
        });

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

        return app;
    }
}
