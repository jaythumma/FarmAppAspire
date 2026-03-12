using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/products").WithTags("Products");

        g.MapGet("/insulated-boxes", async (CustomerDbContext db) =>
        {
            var boxes = await db.InsulatedBoxConfigs.OrderBy(b => b.WeightLbs).ToListAsync();
            return Results.Ok(boxes.Select(b =>
                new InsulatedBoxConfigDto(b.Size, b.WeightLbs, b.BasePricePerLb, b.IsDefault)));
        }).Produces<IEnumerable<InsulatedBoxConfigDto>>();

        g.MapGet("/fedex-tiers", async (CustomerDbContext db) =>
        {
            var tiers = await db.FedExTierConfigs.OrderBy(t => t.WeightOz).ToListAsync();
            return Results.Ok(tiers.Select(t =>
                new FedExTierConfigDto(t.TierSize, t.WeightOz, t.FixedPrice)));
        }).Produces<IEnumerable<FedExTierConfigDto>>();

        return app;
    }
}
