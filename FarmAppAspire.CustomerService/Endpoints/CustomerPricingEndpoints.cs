using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class CustomerPricingEndpoints
{
    public static IEndpointRouteBuilder MapCustomerPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/customers/{customerId:guid}/pricing").WithTags("CustomerPricing");

        g.MapGet("/", async (Guid customerId, CustomerDbContext db) =>
        {
            var pricings = await db.CustomerPricings
                .Where(p => p.CustomerId == customerId)
                .ToListAsync();
            return Results.Ok(pricings.Select(ToDto));
        }).Produces<IEnumerable<CustomerPricingDto>>();

        g.MapPost("/", async (Guid customerId, CreateCustomerPricingRequest req, CustomerDbContext db) =>
        {
            if (!await db.Customers.AnyAsync(c => c.Id == customerId))
                return Results.NotFound();

            var pricing = new CustomerPricing
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                BoxSize = req.BoxSize,
                PricePerLb = req.PricePerLb,
                ShippingRate = req.ShippingRate,
                MinQty = req.MinQty
            };
            db.CustomerPricings.Add(pricing);
            await db.SaveChangesAsync();
            return Results.Created($"/customers/{customerId}/pricing/{pricing.Id}", ToDto(pricing));
        }).Produces<CustomerPricingDto>(StatusCodes.Status201Created);

        g.MapPut("/{priceId:guid}", async (Guid customerId, Guid priceId, UpdateCustomerPricingRequest req, CustomerDbContext db) =>
        {
            var pricing = await db.CustomerPricings
                .FirstOrDefaultAsync(p => p.Id == priceId && p.CustomerId == customerId);
            if (pricing is null) return Results.NotFound();

            pricing.PricePerLb   = req.PricePerLb;
            pricing.ShippingRate = req.ShippingRate;
            pricing.MinQty       = req.MinQty;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(pricing));
        }).Produces<CustomerPricingDto>();

        g.MapDelete("/{priceId:guid}", async (Guid customerId, Guid priceId, CustomerDbContext db) =>
        {
            var pricing = await db.CustomerPricings
                .FirstOrDefaultAsync(p => p.Id == priceId && p.CustomerId == customerId);
            if (pricing is null) return Results.NotFound();
            db.CustomerPricings.Remove(pricing);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static CustomerPricingDto ToDto(CustomerPricing p) =>
        new(p.Id, p.BoxSize, p.PricePerLb, p.ShippingRate, p.MinQty);
}
