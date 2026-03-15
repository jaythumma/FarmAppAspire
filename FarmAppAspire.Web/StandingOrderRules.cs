namespace FarmAppAspire.Web;

/// <summary>
/// UI-side business rules for standing order lifecycle management (Issue #44).
/// </summary>
public static class StandingOrderRules
{
    /// <summary>Returns <c>true</c> when the standing order can be paused.</summary>
    public static bool CanPause(string status) => status == "Active";

    /// <summary>Returns <c>true</c> when the standing order can be resumed.</summary>
    public static bool CanResume(string status) => status == "Paused";

    /// <summary>Returns <c>true</c> when the standing order can be stopped.</summary>
    public static bool CanStop(string status) => status is "Active" or "Paused";

    /// <summary>Returns <c>true</c> when the standing order can be edited (frequency/lines).</summary>
    public static bool CanEdit(string status) => status is "Active" or "Paused";

    /// <summary>
    /// Returns <c>true</c> when a skip-week request is allowed for <paramref name="weekOf"/>.
    /// Skip requests must be submitted before end-of-day Friday of the harvest week
    /// (the Friday that precedes the Monday shipment date).
    /// </summary>
    public static bool CanAddSkip(DateTime weekOf, DateTime utcNow)
    {
        var monday = MondayOf(weekOf);
        var cutoff = monday.AddDays(-3);  // preceding Friday
        return utcNow.Date <= cutoff.Date;
    }

    /// <summary>
    /// Returns a Bootstrap badge CSS class for the given standing order status.
    /// </summary>
    public static string StatusBadgeClass(string status) => status switch
    {
        "Active"  => "bg-success",
        "Paused"  => "bg-warning text-dark",
        "Stopped" => "bg-secondary",
        _         => "bg-light text-dark"
    };

    /// <summary>Normalises any date to the Monday of its week (UTC).</summary>
    private static DateTime MondayOf(DateTime date)
    {
        var d = date.Date;
        var diff = (int)d.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        return d.AddDays(-diff);
    }
}
