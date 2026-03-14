using FarmAppAspire.FarmService.Models;

namespace FarmAppAspire.Tests.Unit.FarmService;

public class FieldMappingTests
{
    private static List<GpsPoint> SamplePolygon() =>
    [
        new GpsPoint(40.123, -84.123),
        new GpsPoint(40.124, -84.122),
        new GpsPoint(40.125, -84.121),
    ];

    private static Field BuildField(FieldStatus status = FieldStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        FarmId = Guid.NewGuid(),
        Name = "North Block A",
        Code = "NBA",
        AreaHa = 12.5m,
        GpsPolygon = SamplePolygon(),
        SoilType = "loam",
        Status = status,
        CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
        CreatedBy = "test-user",
        ModifiedAt = null,
        ModifiedBy = null,
    };

    [Fact]
    public void ToDto_MapsAllFields()
    {
        var field = BuildField();

        var dto = field.ToDto();

        Assert.Equal(field.Id, dto.Id);
        Assert.Equal(field.FarmId, dto.FarmId);
        Assert.Equal(field.Name, dto.Name);
        Assert.Equal(field.Code, dto.Code);
        Assert.Equal(field.AreaHa, dto.AreaHa);
        Assert.Equal(field.SoilType, dto.SoilType);
        Assert.Equal(field.Status, dto.Status);
        Assert.Equal(field.CreatedAt, dto.CreatedAt);
        Assert.Null(dto.ModifiedAt);
    }

    [Fact]
    public void ToDto_MapsGpsPolygon()
    {
        var field = BuildField();

        var dto = field.ToDto();

        Assert.Equal(3, dto.GpsPolygon.Count);
        Assert.Equal(40.123, dto.GpsPolygon[0].Lat);
        Assert.Equal(-84.123, dto.GpsPolygon[0].Lng);
    }

    [Fact]
    public void ToDto_MapsModifiedAt_WhenSet()
    {
        var field = BuildField();
        field.ModifiedAt = new DateTime(2024, 2, 1, 8, 0, 0, DateTimeKind.Utc);

        var dto = field.ToDto();

        Assert.NotNull(dto.ModifiedAt);
        Assert.Equal(field.ModifiedAt, dto.ModifiedAt);
    }

    [Fact]
    public void ToDto_MapsInactiveStatus()
    {
        var field = BuildField(FieldStatus.Inactive);

        var dto = field.ToDto();

        Assert.Equal(FieldStatus.Inactive, dto.Status);
    }

    [Fact]
    public void GpsPoint_Record_EqualityByValue()
    {
        var p1 = new GpsPoint(40.123, -84.123);
        var p2 = new GpsPoint(40.123, -84.123);
        var p3 = new GpsPoint(40.999, -84.999);

        Assert.Equal(p1, p2);
        Assert.NotEqual(p1, p3);
    }

    [Fact]
    public void ToDto_EmptyGpsPolygon_MapsToEmptyList()
    {
        var field = BuildField();
        field.GpsPolygon = [];

        var dto = field.ToDto();

        Assert.NotNull(dto.GpsPolygon);
        Assert.Empty(dto.GpsPolygon);
    }
}
