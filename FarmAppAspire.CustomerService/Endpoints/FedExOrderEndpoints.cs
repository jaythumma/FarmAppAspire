using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class FedExOrderEndpoints
{
    public static IEndpointRouteBuilder MapFedExOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/customers/{customerId:guid}/fedex-orders").WithTags("FedExOrders");

        g.MapGet("/", async (Guid customerId, CustomerDbContext db) =>
        {
            var instances = await db.OrderInstances
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customerId && i.Channel == OrderChannel.FedEx)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
            return Results.Ok(instances.Select(OrderInstanceEndpoints.ToDto));
        }).Produces<IEnumerable<OrderInstanceDto>>();

        g.MapGet("/{instanceId:guid}", async (Guid customerId, Guid instanceId, CustomerDbContext db) =>
        {
            var i = await db.OrderInstances.Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == instanceId
                                       && i.CustomerId == customerId
                                       && i.Channel == OrderChannel.FedEx);
            return i is null ? Results.NotFound() : Results.Ok(OrderInstanceEndpoints.ToDto(i));
        }).Produces<OrderInstanceDto>();

        g.MapPost("/", async (Guid customerId, CreateFedExOrderRequest req,
            CustomerDbContext db, HttpContext ctx) =>
        {
            if (!await db.Customers.AnyAsync(c => c.Id == customerId))
                return Results.NotFound();

            if (!req.Lines.Any())
                return Results.BadRequest("At least one line is required.");

            if (req.ContactId.HasValue &&
                !await db.CustomerContacts.AnyAsync(c => c.Id == req.ContactId && c.CustomerId == customerId))
                return Results.BadRequest("ContactId does not belong to this customer.");

            var tierConfigs = await db.FedExTierConfigs.ToListAsync();
            var userId = ctx.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";

            var lines = req.Lines.Select(l =>
            {
                var config   = tierConfigs.First(t => t.TierSize == l.TierSize);
                var totalOz  = config.WeightOz * l.Qty;
                var pkgType  = PriceResolutionService.DerivePackagingType(totalOz);
                return new OrderInstanceLine
                {
                    Id             = Guid.NewGuid(),
                    FedExTierSize  = l.TierSize,
                    FedExFixedPrice = config.FixedPrice,
                    Qty            = l.Qty,
                    PackagingType  = pkgType
                };
            }).ToList();

            var instance = new OrderInstance
            {
                Id        = Guid.NewGuid(),
                CustomerId = customerId,
                ContactId  = req.ContactId,
                Channel   = OrderChannel.FedEx,
                Status    = OrderInstanceStatus.Pending,
                WeekOf    = DateTime.UtcNow.Date,
                IsSample  = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                Lines     = lines
            };

            db.OrderInstances.Add(instance);
            await db.SaveChangesAsync();
            return Results.Created($"/customers/{customerId}/fedex-orders/{instance.Id}",
                OrderInstanceEndpoints.ToDto(instance));
        }).Produces<OrderInstanceDto>(StatusCodes.Status201Created);

        return app;
    }
}
