using FarmAppAspire.CustomerService.Models;

namespace FarmAppAspire.CustomerService.Services;

/// <summary>
/// Pure stateless logic for determining whether a standing order should generate
/// an instance for a given week, and for BiWeekly rhythm management.
/// </summary>
public static class StandingOrderScheduleService
{
    /// <summary>
    /// Returns true if a standing order should generate an instance for <paramref name="weekOf"/>.
    /// All dates should be Mondays.
    /// </summary>
    public static bool ShouldGenerateForWeek(
        OrderFrequency frequency,
        DateTime startWeek,
        IEnumerable<DateTime> skippedWeeks,
        DateTime? lastShippedWeek,
        DateTime weekOf,
        MonthlyWeek? monthlyWeek = null)
    {
        if (weekOf < startWeek) return false;
        if (skippedWeeks.Any(s => s.Date == weekOf.Date)) return false;

        return frequency switch
        {
            OrderFrequency.Weekly    => true,
            OrderFrequency.BiWeekly  => IsBiWeeklyShipWeek(startWeek, skippedWeeks, lastShippedWeek, weekOf),
            OrderFrequency.Monthly   => monthlyWeek.HasValue && IsNthMondayOfMonth(weekOf, monthlyWeek.Value),
            OrderFrequency.OnRequest => false, // OnRequest is triggered manually, not by schedule
            OrderFrequency.Stopped   => false,
            _ => false
        };
    }

    /// <summary>
    /// Determines the nth Monday of the month (1=first, 2=second, etc.) for a given date.
    /// </summary>
    public static bool IsNthMondayOfMonth(DateTime monday, MonthlyWeek nth)
    {
        if (monday.DayOfWeek != DayOfWeek.Monday) return false;
        var weekNumber = (monday.Day - 1) / 7 + 1;
        return weekNumber == (int)nth;
    }

    // ── BiWeekly logic ────────────────────────────────────────────────────────

    private static bool IsBiWeeklyShipWeek(
        DateTime startWeek,
        IEnumerable<DateTime> skippedWeeks,
        DateTime? lastShippedWeek,
        DateTime weekOf)
    {
        // Biweekly rhythm is always anchored at startWeek.
        // Skips are exclusions only — they don't change the alternating cadence.
        var weeksDiff = (int)((weekOf.Date - startWeek.Date).TotalDays / 7);
        return weeksDiff >= 0 && weeksDiff % 2 == 0;
    }
}
