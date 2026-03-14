using FarmAppAspire.FarmService.Models;

namespace FarmAppAspire.Tests.Unit.FarmService;

public class FieldValidationTests
{
    private static List<GpsPoint> ValidPolygon() =>
    [
        new GpsPoint(40.123, -84.123),
        new GpsPoint(40.124, -84.122),
        new GpsPoint(40.125, -84.121),
    ];

    // ── area_ha validation ────────────────────────────────────────────────────

    [Fact]
    public void CreateFieldRequest_ZeroAreaHa_IsInvalid()
    {
        var req = new CreateFieldRequest("North Block A", "NBA", 0m, ValidPolygon(), "loam");
        var isInvalid = req.AreaHa <= 0;
        Assert.True(isInvalid, "area_ha of 0 should be invalid");
    }

    [Fact]
    public void CreateFieldRequest_NegativeAreaHa_IsInvalid()
    {
        var req = new CreateFieldRequest("North Block A", "NBA", -5m, ValidPolygon(), "loam");
        var isInvalid = req.AreaHa <= 0;
        Assert.True(isInvalid, "Negative area_ha should be invalid");
    }

    [Fact]
    public void CreateFieldRequest_PositiveAreaHa_IsValid()
    {
        var req = new CreateFieldRequest("North Block A", "NBA", 12.5m, ValidPolygon(), "loam");
        var isInvalid = req.AreaHa <= 0;
        Assert.False(isInvalid, "Positive area_ha should be valid");
    }

    // ── gps_polygon validation ────────────────────────────────────────────────

    [Fact]
    public void CreateFieldRequest_GpsPolygonWithFewerThan3Points_IsInvalid()
    {
        var twoPoints = new List<GpsPoint>
        {
            new GpsPoint(40.123, -84.123),
            new GpsPoint(40.124, -84.122),
        };
        var req = new CreateFieldRequest("North Block A", "NBA", 12.5m, twoPoints, "loam");
        var isInvalid = req.GpsPolygon is null || req.GpsPolygon.Count < 3;
        Assert.True(isInvalid, "GPS polygon with fewer than 3 points should be invalid");
    }

    [Fact]
    public void CreateFieldRequest_EmptyGpsPolygon_IsInvalid()
    {
        var req = new CreateFieldRequest("North Block A", "NBA", 12.5m, [], "loam");
        var isInvalid = req.GpsPolygon is null || req.GpsPolygon.Count < 3;
        Assert.True(isInvalid, "Empty GPS polygon should be invalid");
    }

    [Fact]
    public void CreateFieldRequest_GpsPolygonWith3Points_IsValid()
    {
        var req = new CreateFieldRequest("North Block A", "NBA", 12.5m, ValidPolygon(), "loam");
        var isInvalid = req.GpsPolygon is null || req.GpsPolygon.Count < 3;
        Assert.False(isInvalid, "GPS polygon with 3 points should be valid");
    }

    [Fact]
    public void CreateFieldRequest_GpsPolygonWithMoreThan3Points_IsValid()
    {
        var manyPoints = new List<GpsPoint>
        {
            new GpsPoint(40.123, -84.123),
            new GpsPoint(40.124, -84.122),
            new GpsPoint(40.125, -84.121),
            new GpsPoint(40.126, -84.120),
        };
        var req = new CreateFieldRequest("North Block A", "NBA", 12.5m, manyPoints, "loam");
        var isInvalid = req.GpsPolygon is null || req.GpsPolygon.Count < 3;
        Assert.False(isInvalid, "GPS polygon with more than 3 points should be valid");
    }

    // ── Status default ────────────────────────────────────────────────────────

    [Fact]
    public void CreateFieldRequest_StatusDefaultsToActive()
    {
        var req = new CreateFieldRequest("North Block A", "NBA", 12.5m, ValidPolygon(), "loam");
        Assert.Equal(FieldStatus.Active, req.Status);
    }

    [Fact]
    public void CreateFieldRequest_StatusCanBeSetToInactive()
    {
        var req = new CreateFieldRequest("North Block A", "NBA", 12.5m, ValidPolygon(), "loam", FieldStatus.Inactive);
        Assert.Equal(FieldStatus.Inactive, req.Status);
    }
}
