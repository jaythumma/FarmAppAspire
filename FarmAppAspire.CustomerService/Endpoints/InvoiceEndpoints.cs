using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.CustomerService.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/customers/{customerId:guid}/invoices").WithTags("Invoices");

        g.MapGet("/", async (Guid customerId, CustomerDbContext db,
            int? seasonYear = null, string? channel = null) =>
        {
            var q = db.Invoices.Where(i => i.CustomerId == customerId);
            if (seasonYear.HasValue) q = q.Where(i => i.SeasonYear == seasonYear);
            if (Enum.TryParse<OrderChannel>(channel, ignoreCase: true, out var ch))
                q = q.Where(i => i.Channel == ch);

            var invoices = await q.OrderBy(i => i.SeasonYear).ThenBy(i => i.SeekNum).ToListAsync();
            return Results.Ok(invoices.Select(ToDto));
        }).Produces<IEnumerable<InvoiceDto>>();

        g.MapGet("/{invoiceId:guid}", async (Guid customerId, Guid invoiceId, CustomerDbContext db) =>
        {
            var inv = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CustomerId == customerId);
            return inv is null ? Results.NotFound() : Results.Ok(ToDto(inv));
        }).Produces<InvoiceDto>();

        return app;
    }

    private static InvoiceDto ToDto(Invoice i) =>
        new(i.Id, i.OrderInstanceId, i.CustomerId, i.Channel,
            i.SeasonYear, i.SeekNum, i.Label, i.CreatedAt);
}
