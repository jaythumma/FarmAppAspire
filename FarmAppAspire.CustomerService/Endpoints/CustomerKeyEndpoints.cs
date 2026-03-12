using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class CustomerKeyEndpoints
{
    public static IEndpointRouteBuilder MapCustomerKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/customers/{customerId:guid}/key").WithTags("CustomerKey");

        g.MapPost("/generate", async (Guid customerId, CustomerDbContext db) =>
        {
            var customer = await db.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer is null) return Results.NotFound();

            var svc = new CustomerKeyService();
            var proposed = svc.Generate(customer);

            var existing = await db.Customers
                .Where(c => c.CustomerKey != null)
                .Select(c => new { c.CustomerKey, c.Id })
                .ToListAsync();

            var collision = CustomerKeyService.CheckCollision(
                proposed, customerId,
                existing.Select(e => (e.CustomerKey!, e.Id)));

            customer.CustomerKey      = proposed;
            customer.CustomerKeyCollision = collision;
            await db.SaveChangesAsync();

            return collision
                ? Results.Conflict(new CustomerKeyDto(proposed, HasCollision: true))
                : Results.Ok(new CustomerKeyDto(proposed, HasCollision: false));
        }).Produces<CustomerKeyDto>().Produces(StatusCodes.Status409Conflict);

        g.MapPut("/", async (Guid customerId, CustomerKeyDto req, CustomerDbContext db) =>
        {
            var customer = await db.Customers.FindAsync(customerId);
            if (customer is null) return Results.NotFound();

            var existing = await db.Customers
                .Where(c => c.CustomerKey != null && c.Id != customerId)
                .Select(c => new { c.CustomerKey, c.Id })
                .ToListAsync();

            var collision = CustomerKeyService.CheckCollision(
                req.CustomerKey!, customerId,
                existing.Select(e => (e.CustomerKey!, e.Id)));

            if (collision) return Results.Conflict("CustomerKey already in use by another customer.");

            customer.CustomerKey      = req.CustomerKey;
            customer.CustomerKeyCollision = false;
            await db.SaveChangesAsync();
            return Results.Ok(new CustomerKeyDto(customer.CustomerKey, false));
        }).Produces<CustomerKeyDto>();

        return app;
    }
}
