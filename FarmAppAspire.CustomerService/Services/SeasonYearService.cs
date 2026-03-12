namespace FarmAppAspire.CustomerService.Services;

/// <summary>Stateless helpers for farm season year and boundary date calculations.</summary>
public static class SeasonYearService
{
    /// <summary>
    /// Returns the season year for a given date.
    /// Season year = the calendar year in which the current season began (June start year).
    /// Season runs: first Monday of June(Y) → last Monday of May(Y+1).
    /// </summary>
    public static int CurrentSeasonYear(DateTime date)
    {
        // If date >= first Monday of June this year, season year = this year
        var firstJuneMonday = FirstMondayOfJune(date.Year);
        return date >= firstJuneMonday ? date.Year : date.Year - 1;
    }

    /// <summary>Returns the first Monday of June for the given year.</summary>
    public static DateTime FirstMondayOfJune(int year)
    {
        var june1 = new DateTime(year, 6, 1);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)june1.DayOfWeek + 7) % 7;
        return june1.AddDays(daysUntilMonday);
    }

    /// <summary>Returns the last Monday of May following the given season year (= season end).</summary>
    public static DateTime LastMondayOfMayFollowing(int seasonYear)
    {
        var may31 = new DateTime(seasonYear + 1, 5, 31);
        var daysBack = ((int)may31.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return may31.AddDays(-daysBack);
    }

    /// <summary>Returns the Monday of the week containing the given date.</summary>
    public static DateTime MondayOf(DateTime date)
    {
        var daysBack = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.Date.AddDays(-daysBack);
    }
}
