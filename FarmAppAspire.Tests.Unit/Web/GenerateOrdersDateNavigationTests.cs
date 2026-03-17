namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Validates the prev/next-week navigation logic used on the Admin Orders page
/// for the Generate Orders date picker.
/// </summary>
public class GenerateOrdersDateNavigationTests
{
    // ── Helper that mirrors the private SnapToMonday method ───────────────────

    private static DateTime SnapToMonday(DateTime date)
    {
        var days = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.Date.AddDays(-days);
    }

    private static DateTime GeneratePreviousWeek(DateTime current)
        => SnapToMonday(current).AddDays(-7);

    private static DateTime GenerateNextWeek(DateTime current)
        => SnapToMonday(current).AddDays(7);

    // ── PreviousWeek ──────────────────────────────────────────────────────────

    [Fact]
    public void GeneratePreviousWeek_FromMonday_SubtractsSeven()
    {
        var monday = new DateTime(2025, 4, 7); // known Monday
        var result = GeneratePreviousWeek(monday);
        Assert.Equal(new DateTime(2025, 3, 31), result);
    }

    [Fact]
    public void GeneratePreviousWeek_FromMidWeek_SnapsToMondayThenSubtractsSeven()
    {
        var wednesday = new DateTime(2025, 4, 9); // Wednesday in week of Mon 7 Apr
        var result    = GeneratePreviousWeek(wednesday);
        Assert.Equal(new DateTime(2025, 3, 31), result); // Mon of the previous week
    }

    [Fact]
    public void GeneratePreviousWeek_AlwaysLandsOnMonday()
    {
        var sunday = new DateTime(2025, 4, 13); // Sunday
        var result = GeneratePreviousWeek(sunday);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }

    // ── NextWeek ──────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateNextWeek_FromMonday_AddsSeven()
    {
        var monday = new DateTime(2025, 4, 7);
        var result = GenerateNextWeek(monday);
        Assert.Equal(new DateTime(2025, 4, 14), result);
    }

    [Fact]
    public void GenerateNextWeek_FromMidWeek_SnapsToMondayThenAddsSeven()
    {
        var friday = new DateTime(2025, 4, 11); // Friday in week of Mon 7 Apr
        var result = GenerateNextWeek(friday);
        Assert.Equal(new DateTime(2025, 4, 14), result); // Mon of the next week
    }

    [Fact]
    public void GenerateNextWeek_AlwaysLandsOnMonday()
    {
        var thursday = new DateTime(2025, 4, 10);
        var result   = GenerateNextWeek(thursday);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public void PreviousThenNext_ReturnsToOriginalMonday()
    {
        var monday  = new DateTime(2025, 4, 7);
        var stepped = GenerateNextWeek(GeneratePreviousWeek(monday));
        Assert.Equal(monday, stepped);
    }

    [Fact]
    public void NextThenPrevious_ReturnsToOriginalMonday()
    {
        var monday  = new DateTime(2025, 4, 7);
        var stepped = GeneratePreviousWeek(GenerateNextWeek(monday));
        Assert.Equal(monday, stepped);
    }
}
