using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class AdminGenerateInstancesTests
{
    // ── MondayOf snap logic (mirrors the server-side SeasonYearService) ──────

    [Theory]
    [InlineData(2025, 4, 7)]   // Monday → stays Monday
    [InlineData(2025, 4, 8)]   // Tuesday → snaps to Monday Apr 7
    [InlineData(2025, 4, 9)]   // Wednesday → snaps to Monday Apr 7
    [InlineData(2025, 4, 10)]  // Thursday → snaps to Monday Apr 7
    [InlineData(2025, 4, 11)]  // Friday → snaps to Monday Apr 7
    [InlineData(2025, 4, 12)]  // Saturday → snaps to Monday Apr 7
    [InlineData(2025, 4, 13)]  // Sunday → snaps to Monday Apr 7
    public void MondayOf_AlwaysReturnsMonday(int year, int month, int day)
    {
        var date   = new DateTime(year, month, day);
        var monday = SeasonYearService.MondayOf(date);

        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
    }

    [Theory]
    [InlineData(2025, 4, 7,  2025, 4, 7)]   // Monday → same Monday
    [InlineData(2025, 4, 8,  2025, 4, 7)]   // Tuesday → previous Monday
    [InlineData(2025, 4, 13, 2025, 4, 7)]   // Sunday → previous Monday
    [InlineData(2025, 4, 14, 2025, 4, 14)]  // Monday (next week) → stays
    public void MondayOf_SnapsToCorrectMonday(
        int year, int month, int day,
        int expYear, int expMonth, int expDay)
    {
        var date     = new DateTime(year, month, day);
        var expected = new DateTime(expYear, expMonth, expDay);

        Assert.Equal(expected, SeasonYearService.MondayOf(date));
    }

    [Fact]
    public void MondayOf_PreservesDateOnly_NotTime()
    {
        var date   = new DateTime(2025, 4, 9, 14, 30, 0); // Wednesday with time
        var monday = SeasonYearService.MondayOf(date);

        Assert.Equal(TimeSpan.Zero, monday.TimeOfDay);
    }

    // ── GenerateInstancesResponse DTO shape ───────────────────────────────────

    [Fact]
    public void GenerateInstancesResponse_TracksCreatedAndSkippedCounts()
    {
        var weekOf    = new DateTime(2025, 4, 7);
        var instances = new List<FarmAppAspire.CustomerService.Models.GeneratedInstanceSummary>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Alpha Farm", "AF-001", "Insulated", weekOf, false, 2, 120m),
            new(Guid.NewGuid(), Guid.NewGuid(), "Beta CSA",  null,     "Insulated", weekOf, true,  1, 0m)
        };

        var response = new FarmAppAspire.CustomerService.Models.GenerateInstancesResponse(
            GeneratedCount: 2,
            SkippedCount:   3,
            WeekOf:         weekOf,
            Instances:      instances);

        Assert.Equal(2, response.GeneratedCount);
        Assert.Equal(3, response.SkippedCount);
        Assert.Equal(weekOf, response.WeekOf);
        Assert.Equal(2, response.Instances.Count);
    }

    [Fact]
    public void GeneratedInstanceSummary_NullCustomerKey_IsAllowed()
    {
        var summary = new FarmAppAspire.CustomerService.Models.GeneratedInstanceSummary(
            InstanceId:          Guid.NewGuid(),
            CustomerId:          Guid.NewGuid(),
            CustomerDisplayName: "No Key Farm",
            CustomerKey:         null,
            Channel:             "Insulated",
            WeekOf:              new DateTime(2025, 4, 7),
            IsSample:            false,
            TotalQty:            1,
            TotalAmount:         50m);

        Assert.Null(summary.CustomerKey);
    }
}
