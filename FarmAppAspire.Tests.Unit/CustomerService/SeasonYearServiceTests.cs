using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class SeasonYearServiceTests
{
    private static DateTime Monday(int year, int month, int day) => new(year, month, day);

    [Theory]
    [InlineData(2024, 7, 15, 2024)]   // July — well into 2024 season
    [InlineData(2024, 6, 3,  2024)]   // First Monday of June 2024 = season start
    [InlineData(2025, 2, 10, 2024)]   // February 2025 is still 2024 season
    [InlineData(2025, 5, 26, 2024)]   // Last Monday of May 2025 — still 2024 season
    [InlineData(2025, 6, 2,  2025)]   // First Monday of June 2025 = new season
    [InlineData(2023, 8, 1,  2023)]   // August 2023 = 2023 season
    public void CurrentSeasonYear_ReturnsCorrectYear(int year, int month, int day, int expectedSeason)
    {
        var date = new DateTime(year, month, day);
        Assert.Equal(expectedSeason, SeasonYearService.CurrentSeasonYear(date));
    }

    [Theory]
    [InlineData(2024, 6, 3)]   // 2024
    [InlineData(2025, 6, 2)]   // 2025
    [InlineData(2023, 6, 5)]   // 2023
    public void FirstMondayOfJune_IsCorrect(int year, int month, int day)
    {
        var result = SeasonYearService.FirstMondayOfJune(year);
        Assert.Equal(new DateTime(year, month, day), result);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }

    [Theory]
    [InlineData(2024, 2025, 5, 26)]  // Last Monday of May 2025 = end of 2024 season
    [InlineData(2023, 2024, 5, 27)]  // Last Monday of May 2024
    public void LastMondayOfMay_IsCorrect(int seasonYear, int year, int month, int day)
    {
        var result = SeasonYearService.LastMondayOfMayFollowing(seasonYear);
        Assert.Equal(new DateTime(year, month, day), result);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }
}
