using FarmAppAspire.FarmService.Data;
using FarmAppAspire.FarmService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.FarmService.Endpoints;

public static class FieldEndpoints
{
    public static IEndpointRouteBuilder MapFieldEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/farms").WithTags("Fields");

        // GET /api/farms/{farmId}/fields
        group.MapGet("{farmId:guid}/fields", async (Guid farmId, FarmDbContext db, CancellationToken ct) =>
        {
            if (!await db.Farms.AnyAsync(f => f.Id == farmId, ct))
                return Results.NotFound();

            var fields = await db.Fields
                .Where(f => f.FarmId == farmId)
                .OrderBy(f => f.Name)
                .Select(f => f.ToDto())
                .ToListAsync(ct);

            return Results.Ok(fields);
        });

        // GET /api/farms/{farmId}/fields/{fieldId}
        group.MapGet("{farmId:guid}/fields/{fieldId:guid}", async (Guid farmId, Guid fieldId, FarmDbContext db, CancellationToken ct) =>
        {
            var field = await db.Fields
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.FarmId == farmId, ct);

            return field is null ? Results.NotFound() : Results.Ok(field.ToDto());
        });

        // POST /api/farms/{farmId}/fields
        group.MapPost("{farmId:guid}/fields", async (Guid farmId, CreateFieldRequest req, FarmDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            if (!await db.Farms.AnyAsync(f => f.Id == farmId, ct))
                return Results.NotFound();

            if (req.AreaHa <= 0)
                return Results.Problem("area_ha must be greater than 0.", statusCode: StatusCodes.Status400BadRequest);

            if (req.GpsPolygon is null || req.GpsPolygon.Count < 3)
                return Results.Problem("gps_polygon must have at least 3 points.", statusCode: StatusCodes.Status400BadRequest);

            var userId = ctx.Request.Headers["X-User-Id"].FirstOrDefault() ?? "unknown";
            var now = DateTime.UtcNow;

            var field = new Field
            {
                Id = Guid.NewGuid(),
                FarmId = farmId,
                Name = req.Name,
                Code = req.Code,
                AreaHa = req.AreaHa,
                GpsPolygon = req.GpsPolygon,
                SoilType = req.SoilType,
                Status = req.Status,
                CreatedAt = now,
                CreatedBy = userId,
            };

            db.Fields.Add(field);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Results.Problem($"A field with code '{req.Code}' already exists in this farm.", statusCode: StatusCodes.Status409Conflict);
            }

            return Results.Created($"/api/farms/{farmId}/fields/{field.Id}", field.ToDto());
        });

        // PUT /api/farms/{farmId}/fields/{fieldId}
        group.MapPut("{farmId:guid}/fields/{fieldId:guid}", async (Guid farmId, Guid fieldId, UpdateFieldRequest req, FarmDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var field = await db.Fields
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.FarmId == farmId, ct);

            if (field is null) return Results.NotFound();

            if (req.AreaHa <= 0)
                return Results.Problem("area_ha must be greater than 0.", statusCode: StatusCodes.Status400BadRequest);

            if (req.GpsPolygon is null || req.GpsPolygon.Count < 3)
                return Results.Problem("gps_polygon must have at least 3 points.", statusCode: StatusCodes.Status400BadRequest);

            field.Name = req.Name;
            field.Code = req.Code;
            field.AreaHa = req.AreaHa;
            field.GpsPolygon = req.GpsPolygon;
            field.SoilType = req.SoilType;
            field.Status = req.Status;
            field.ModifiedAt = DateTime.UtcNow;
            field.ModifiedBy = ctx.Request.Headers["X-User-Id"].FirstOrDefault() ?? "unknown";

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Results.Problem($"A field with code '{req.Code}' already exists in this farm.", statusCode: StatusCodes.Status409Conflict);
            }

            return Results.Ok(field.ToDto());
        });

        // DELETE /api/farms/{farmId}/fields/{fieldId}
        group.MapDelete("{farmId:guid}/fields/{fieldId:guid}", async (Guid farmId, Guid fieldId, FarmDbContext db, CancellationToken ct) =>
        {
            var field = await db.Fields
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.FarmId == farmId, ct);

            if (field is null) return Results.NotFound();

            db.Fields.Remove(field);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }
}
