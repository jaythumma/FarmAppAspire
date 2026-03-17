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

    private const int DaysPerWeek = 7;

    private static bool IsBiWeeklyShipWeek(
        DateTime startWeek,
        IEnumerable<DateTime> skippedWeeks,
        DateTime? lastShippedWeek,
        DateTime weekOf)
    {
        // Walk through skips in chronological order to find the effective cadence anchor.
        // When a skip falls on a scheduled biweekly slot, the rhythm restarts from
        // skip+1 week (the "resume point"). The first ship in the restarted cadence
        // is resume+2 weeks, i.e., weekOf must be strictly more than 0 and even-spaced
        // from the new anchor (W+3 from the skipped week, not W+2).
        var orderedSkips = skippedWeeks.Select(s => s.Date).OrderBy(s => s);

        DateTime anchor = startWeek;
        bool resumeMode = false; // false: anchor itself ships; true: first ship is anchor+2

        foreach (var skip in orderedSkips)
        {
            if (skip >= weekOf.Date) break; // skips at/after target don't affect this week

            var diff = (int)((skip - anchor).TotalDays / DaysPerWeek);
            bool onSchedule = resumeMode
                ? diff > 0 && diff % 2 == 0
                : diff >= 0 && diff % 2 == 0;

            if (onSchedule)
            {
                anchor = skip.AddDays(DaysPerWeek); // resume point = Monday immediately after the skip
                resumeMode = true;                  // anchor is not a ship week; first ship is anchor+2
            }
        }

        var weeksDiff = (int)((weekOf.Date - anchor.Date).TotalDays / DaysPerWeek);
        return resumeMode
            ? weeksDiff > 0 && weeksDiff % 2 == 0   // W+3, W+5, … from anchor
            : weeksDiff >= 0 && weeksDiff % 2 == 0;  // W1, W3, W5, … from startWeek
    }
}
