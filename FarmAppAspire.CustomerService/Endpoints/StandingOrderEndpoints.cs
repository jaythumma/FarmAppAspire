using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class StandingOrderEndpoints
{
    public static IEndpointRouteBuilder MapStandingOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/customers/{customerId:guid}/standing-orders").WithTags("StandingOrders");

        g.MapGet("/", async (Guid customerId, CustomerDbContext db) =>
        {
            var orders = await db.StandingOrders
                .Include(s => s.Lines)
                .Include(s => s.Skips)
                .Where(s => s.CustomerId == customerId)
                .ToListAsync();

            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            var overrides  = await db.CustomerPricings.Where(p => p.CustomerId == customerId).ToListAsync();

            return Results.Ok(orders.Select(s => ToDto(s, boxConfigs, overrides)));
        }).Produces<IEnumerable<StandingOrderDto>>();

        g.MapGet("/{soId:guid}", async (Guid customerId, Guid soId, CustomerDbContext db) =>
        {
            var so = await db.StandingOrders
                .Include(s => s.Lines)
                .Include(s => s.Skips)
                .FirstOrDefaultAsync(s => s.Id == soId && s.CustomerId == customerId);
            if (so is null) return Results.NotFound();

            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            var overrides  = await db.CustomerPricings.Where(p => p.CustomerId == customerId).ToListAsync();
            return Results.Ok(ToDto(so, boxConfigs, overrides));
        }).Produces<StandingOrderDto>();

        g.MapPost("/", async (Guid customerId, CreateStandingOrderRequest req,
            CustomerDbContext db, HttpContext ctx) =>
        {
            if (!await db.Customers.AnyAsync(c => c.Id == customerId))
                return Results.NotFound();

            if (!req.Lines.Any())
                return Results.BadRequest("At least one order line is required.");

            // Validate contact belongs to customer
            if (req.ContactId.HasValue &&
                !await db.CustomerContacts.AnyAsync(c => c.Id == req.ContactId && c.CustomerId == customerId))
                return Results.BadRequest("ContactId does not belong to this customer.");

            // Single active standing order rule
            var hasActive = await db.StandingOrders.AnyAsync(s =>
                s.CustomerId == customerId &&
                s.Status != StandingOrderStatus.Stopped &&
                s.Frequency != OrderFrequency.Stopped);
            if (hasActive) return Results.Conflict("Customer already has an active standing order.");

            var userId  = ctx.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
            var monday  = SeasonYearService.MondayOf(DateTime.UtcNow);
            var season  = SeasonYearService.CurrentSeasonYear(DateTime.UtcNow);

            var so = new StandingOrder
            {
                Id         = Guid.NewGuid(),
                CustomerId = customerId,
                ContactId  = req.ContactId,
                Frequency  = req.Frequency,
                MonthlyWeek = req.MonthlyWeek,
                IsSample   = req.IsSample,
                SeasonYear = season,
                StartWeek  = monday,
                Status     = StandingOrderStatus.Active,
                CreatedAt  = DateTime.UtcNow,
                CreatedBy  = userId,
                Lines      = req.Lines.Select(l => new StandingOrderLine
                {
                    Id = Guid.NewGuid(), BoxSize = l.BoxSize, Qty = l.Qty
                }).ToList()
            };

            db.StandingOrders.Add(so);
            await db.SaveChangesAsync();

            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            var overrides  = await db.CustomerPricings.Where(p => p.CustomerId == customerId).ToListAsync();
            return Results.Created($"/customers/{customerId}/standing-orders/{so.Id}",
                ToDto(so, boxConfigs, overrides));
        }).Produces<StandingOrderDto>(StatusCodes.Status201Created);

        g.MapPut("/{soId:guid}", async (Guid customerId, Guid soId,
            UpdateStandingOrderRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var so = await db.StandingOrders.Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == soId && s.CustomerId == customerId);
            if (so is null) return Results.NotFound();

            if (req.ContactId.HasValue &&
                !await db.CustomerContacts.AnyAsync(c => c.Id == req.ContactId && c.CustomerId == customerId))
                return Results.BadRequest("ContactId does not belong to this customer.");

            so.Frequency    = req.Frequency;
            so.MonthlyWeek  = req.MonthlyWeek;
            so.IsSample     = req.IsSample;
            so.ContactId    = req.ContactId;
            so.ModifiedAt   = DateTime.UtcNow;
            so.ModifiedBy   = ctx.Request.Headers["X-User-Id"].FirstOrDefault();

            db.StandingOrderLines.RemoveRange(so.Lines);
            so.Lines = req.Lines.Select(l => new StandingOrderLine
            {
                Id = Guid.NewGuid(), StandingOrderId = so.Id, BoxSize = l.BoxSize, Qty = l.Qty
            }).ToList();

            await db.SaveChangesAsync();

            var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
            var overrides  = await db.CustomerPricings.Where(p => p.CustomerId == customerId).ToListAsync();
            return Results.Ok(ToDto(so, boxConfigs, overrides));
        }).Produces<StandingOrderDto>();

        g.MapPatch("/{soId:guid}/contact", async (Guid customerId, Guid soId,
            UpdateStandingOrderContactRequest req, CustomerDbContext db, HttpContext ctx) =>
        {
            var so = await db.StandingOrders
                .FirstOrDefaultAsync(s => s.Id == soId && s.CustomerId == customerId);
            if (so is null) return Results.NotFound();

            if (req.ContactId.HasValue &&
                !await db.CustomerContacts.AnyAsync(c => c.Id == req.ContactId && c.CustomerId == customerId))
                return Results.BadRequest("ContactId does not belong to this customer.");

            so.ContactId  = req.ContactId;
            so.ModifiedAt = DateTime.UtcNow;
            so.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();

            await db.SaveChangesAsync();
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);

        g.MapPost("/{soId:guid}/pause", async (Guid customerId, Guid soId, CustomerDbContext db, HttpContext ctx) =>
        {
            var so = await db.StandingOrders.FirstOrDefaultAsync(s => s.Id == soId && s.CustomerId == customerId);
            if (so is null) return Results.NotFound();
            so.Status     = StandingOrderStatus.Paused;
            so.ModifiedAt = DateTime.UtcNow;
            so.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{soId:guid}/resume", async (Guid customerId, Guid soId, CustomerDbContext db, HttpContext ctx) =>
        {
            var so = await db.StandingOrders.FirstOrDefaultAsync(s => s.Id == soId && s.CustomerId == customerId);
            if (so is null) return Results.NotFound();
            so.Status     = StandingOrderStatus.Active;
            so.ModifiedAt = DateTime.UtcNow;
            so.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{soId:guid}/stop", async (Guid customerId, Guid soId, CustomerDbContext db, HttpContext ctx) =>
        {
            var so = await db.StandingOrders.FirstOrDefaultAsync(s => s.Id == soId && s.CustomerId == customerId);
            if (so is null) return Results.NotFound();
            so.Status     = StandingOrderStatus.Stopped;
            so.Frequency  = OrderFrequency.Stopped;
            so.ModifiedAt = DateTime.UtcNow;
            so.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{soId:guid}/skip", async (Guid customerId, Guid soId,
            AddSkipWeekRequest req, CustomerDbContext db) =>
        {
            var so = await db.StandingOrders.FirstOrDefaultAsync(s => s.Id == soId && s.CustomerId == customerId);
            if (so is null) return Results.NotFound();

            // Friday cutoff check: for Monday W, cutoff = Friday of that week
            var weekMonday = SeasonYearService.MondayOf(req.WeekOf);
            var friday = weekMonday.AddDays(-3); // Thursday+1 = Friday before the Monday
            // Actually: Mon - 3 days = Friday of PRIOR week. Skip cutoff is Friday of SAME week.
            // Mon = D7 of harvest week, skip cutoff = D5 (Friday of that same week)
            // weekMonday is the Monday being skipped. Friday of that week = weekMonday - 3.
            var cutoff = weekMonday.AddDays(-3); // Friday before the Monday
            if (DateTime.UtcNow.Date > cutoff.Date)
                return Results.UnprocessableEntity("Skip cutoff has passed. Order will be shipped as scheduled.");

            var skip = new StandingOrderSkip
            {
                Id = Guid.NewGuid(), StandingOrderId = soId,
                WeekOf = weekMonday, CreatedAt = DateTime.UtcNow
            };
            db.StandingOrderSkips.Add(skip);

            // Cancel pending instance for that week if it exists
            var instance = await db.OrderInstances.FirstOrDefaultAsync(i =>
                i.StandingOrderId == soId &&
                i.WeekOf.Date == weekMonday.Date &&
                i.Status == OrderInstanceStatus.Pending);
            if (instance is not null)
                instance.Status = OrderInstanceStatus.Cancelled;

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapDelete("/{soId:guid}/skip/{weekOf}", async (Guid customerId, Guid soId,
            DateTime weekOf, CustomerDbContext db) =>
        {
            var weekMonday = SeasonYearService.MondayOf(weekOf);
            var cutoff = weekMonday.AddDays(-3);
            if (DateTime.UtcNow.Date > cutoff.Date)
                return Results.UnprocessableEntity("Skip cutoff has passed.");

            var skip = await db.StandingOrderSkips.FirstOrDefaultAsync(s =>
                s.StandingOrderId == soId && s.WeekOf.Date == weekMonday.Date);
            if (skip is null) return Results.NotFound();

            db.StandingOrderSkips.Remove(skip);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    internal static StandingOrderDto ToDto(StandingOrder so,
        IEnumerable<InsulatedBoxConfig> boxConfigs,
        IEnumerable<CustomerPricing> overrides)
    {
        var (totalBoxes, totalWeight, totalAmount) =
            PriceResolutionService.ComputeStandingOrderTotals(so.Lines, boxConfigs, overrides);

        return new StandingOrderDto(
            so.Id, so.CustomerId, so.ContactId,
            so.Status, so.Frequency, so.MonthlyWeek,
            so.IsSample, so.SeasonYear, so.StartWeek,
            so.Lines.Select(l => new StandingOrderLineDto(l.Id, l.BoxSize, l.Qty)).ToList(),
            so.Skips.Select(s => new StandingOrderSkipDto(s.Id, s.WeekOf)).ToList(),
            totalBoxes, totalWeight, totalAmount,
            so.CreatedAt, so.CreatedBy);
    }
}
