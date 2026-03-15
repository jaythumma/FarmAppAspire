using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Validates <see cref="StandingOrderRules"/> lifecycle rules (Issue #44).
/// </summary>
public class StandingOrderRulesTests
{
    // ── CanPause ──────────────────────────────────────────────────────────────

    [Fact]
    public void CanPause_ActiveOrder_ReturnsTrue()
    {
        Assert.True(StandingOrderRules.CanPause("Active"));
    }

    [Theory]
    [InlineData("Paused")]
    [InlineData("Stopped")]
    public void CanPause_NonActiveOrder_ReturnsFalse(string status)
    {
        Assert.False(StandingOrderRules.CanPause(status));
    }

    // ── CanResume ─────────────────────────────────────────────────────────────

    [Fact]
    public void CanResume_PausedOrder_ReturnsTrue()
    {
        Assert.True(StandingOrderRules.CanResume("Paused"));
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Stopped")]
    public void CanResume_NonPausedOrder_ReturnsFalse(string status)
    {
        Assert.False(StandingOrderRules.CanResume(status));
    }

    // ── CanStop ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Active")]
    [InlineData("Paused")]
    public void CanStop_ActiveOrPaused_ReturnsTrue(string status)
    {
        Assert.True(StandingOrderRules.CanStop(status));
    }

    [Fact]
    public void CanStop_AlreadyStopped_ReturnsFalse()
    {
        Assert.False(StandingOrderRules.CanStop("Stopped"));
    }

    // ── CanEdit ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Active")]
    [InlineData("Paused")]
    public void CanEdit_ActiveOrPaused_ReturnsTrue(string status)
    {
        Assert.True(StandingOrderRules.CanEdit(status));
    }

    [Fact]
    public void CanEdit_Stopped_ReturnsFalse()
    {
        Assert.False(StandingOrderRules.CanEdit("Stopped"));
    }

    // ── CanAddSkip ────────────────────────────────────────────────────────────

    [Fact]
    public void CanAddSkip_BeforeFridayCutoff_ReturnsTrue()
    {
        // weekOf = Monday June 10, 2024 → cutoff = Friday June 7, 2024
        // nowUtc = Thursday June 6, 2024 → before cutoff → allowed
        var weekOf = new DateTime(2024, 6, 10);
        var now    = new DateTime(2024, 6, 6);  // Thursday
        Assert.True(StandingOrderRules.CanAddSkip(weekOf, now));
    }

    [Fact]
    public void CanAddSkip_OnFridayCutoff_ReturnsTrue()
    {
        // weekOf = Monday June 10, 2024 → cutoff = Friday June 7, 2024
        // nowUtc = Friday June 7, 2024 → same day as cutoff → still allowed
        var weekOf = new DateTime(2024, 6, 10);
        var now    = new DateTime(2024, 6, 7);  // Friday (cutoff day)
        Assert.True(StandingOrderRules.CanAddSkip(weekOf, now));
    }

    [Fact]
    public void CanAddSkip_AfterFridayCutoff_ReturnsFalse()
    {
        // weekOf = Monday June 10, 2024 → cutoff = Friday June 7, 2024
        // nowUtc = Saturday June 8, 2024 → after cutoff → rejected
        var weekOf = new DateTime(2024, 6, 10);
        var now    = new DateTime(2024, 6, 8);  // Saturday
        Assert.False(StandingOrderRules.CanAddSkip(weekOf, now));
    }

    [Fact]
    public void CanAddSkip_OnMondayShipDay_ReturnsFalse()
    {
        // weekOf = Monday June 10, 2024 → cutoff = Friday June 7, 2024
        // nowUtc = Monday June 10, 2024 → too late
        var weekOf = new DateTime(2024, 6, 10);
        var now    = new DateTime(2024, 6, 10);  // Monday (ship day)
        Assert.False(StandingOrderRules.CanAddSkip(weekOf, now));
    }

    [Fact]
    public void CanAddSkip_FarFutureWeek_ReturnsTrue()
    {
        // Any future week from today is always skippable
        var weekOf = DateTime.UtcNow.Date.AddDays(21);
        var now    = DateTime.UtcNow.Date;
        Assert.True(StandingOrderRules.CanAddSkip(weekOf, now));
    }

    [Fact]
    public void CanAddSkip_WeekOfIsNonMonday_StillUsesMondayOfThatWeek()
    {
        // weekOf = Wednesday June 12, 2024 → normalises to Monday June 10, 2024
        // cutoff = Friday June 7, 2024
        // now = Thursday June 6, 2024 → before cutoff → allowed
        var weekOf = new DateTime(2024, 6, 12);  // Wednesday
        var now    = new DateTime(2024, 6, 6);
        Assert.True(StandingOrderRules.CanAddSkip(weekOf, now));
    }

    // ── StatusBadgeClass ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Active",  "bg-success")]
    [InlineData("Paused",  "bg-warning text-dark")]
    [InlineData("Stopped", "bg-secondary")]
    public void StatusBadgeClass_KnownStatus_ReturnsExpectedClass(string status, string expected)
    {
        Assert.Equal(expected, StandingOrderRules.StatusBadgeClass(status));
    }

    [Fact]
    public void StatusBadgeClass_UnknownStatus_ReturnsFallback()
    {
        var cls = StandingOrderRules.StatusBadgeClass("Unknown");
        Assert.Equal("bg-light text-dark", cls);
    }
}
