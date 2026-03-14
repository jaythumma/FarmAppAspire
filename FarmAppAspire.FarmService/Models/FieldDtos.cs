namespace FarmAppAspire.FarmService.Models;

public record FieldDto(
    Guid Id,
    Guid FarmId,
    string Name,
    string Code,
    decimal AreaHa,
    List<GpsPoint> GpsPolygon,
    string? SoilType,
    FieldStatus Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt
);

public record CreateFieldRequest(
    string Name,
    string Code,
    decimal AreaHa,
    List<GpsPoint> GpsPolygon,
    string? SoilType,
    FieldStatus Status = FieldStatus.Active
);

public record UpdateFieldRequest(
    string Name,
    string Code,
    decimal AreaHa,
    List<GpsPoint> GpsPolygon,
    string? SoilType,
    FieldStatus Status
);

public static class FieldMappings
{
    public static FieldDto ToDto(this Field f) => new(
        f.Id, f.FarmId, f.Name, f.Code, f.AreaHa, f.GpsPolygon, f.SoilType, f.Status, f.CreatedAt, f.ModifiedAt
    );
}
