using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class StandingOrderScheduleTests
{
    // ── BiWeekly generation ────────────────────────────────────────────────────

    [Fact]
    public void BiWeekly_NoSkip_AlternatesFromStartWeek()
    {
        var start = new DateTime(2024, 6, 3);   // Monday
        var weeks = new[] { start, start.AddDays(7), start.AddDays(14), start.AddDays(21), start.AddDays(28) };

        var results = weeks.Select(w => StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.BiWeekly, start, skippedWeeks: [], lastShippedWeek: null, weekOf: w)).ToList();

        Assert.True(results[0]);   // week 1 → ship
        Assert.False(results[1]);  // week 2 → skip
        Assert.True(results[2]);   // week 3 → ship
        Assert.False(results[3]);  // week 4 → skip
        Assert.True(results[4]);   // week 5 → ship
    }

    [Fact]
    public void BiWeekly_SkipDisruptsRhythm_ResumesFromSkippedWeek()
    {
        // W1 ships; W3 is the next biweekly slot but is skipped by the customer.
        // Resume point = W4 (Monday immediately after skip). Per spec, the cadence
        // restarts biweekly from the resume point → first ship at W6 (W3+3), NOT W5 (W3+2).
        var start = new DateTime(2024, 6, 3);
        var w3 = start.AddDays(14); // skipped week
        var w4 = start.AddDays(21); // resume point (W+1)
        var w5 = start.AddDays(28); // W+2 — must NOT ship
        var w6 = start.AddDays(35); // W+3 — must ship (biweekly from resume point)

        Assert.False(StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.BiWeekly, start, skippedWeeks: [w3], lastShippedWeek: null, weekOf: w4));
        Assert.False(StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.BiWeekly, start, skippedWeeks: [w3], lastShippedWeek: null, weekOf: w5));
        Assert.True(StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.BiWeekly, start, skippedWeeks: [w3], lastShippedWeek: null, weekOf: w6));
    }

    // ── Monthly nth-Monday ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(2024, 6,  3, MonthlyWeek.First)]   // June 2024: 1st Mon
    [InlineData(2024, 6, 10, MonthlyWeek.Second)]  // June 2024: 2nd Mon
    [InlineData(2024, 6, 17, MonthlyWeek.Third)]
    [InlineData(2024, 6, 24, MonthlyWeek.Fourth)]
    [InlineData(2024, 7,  1, MonthlyWeek.First)]   // July 2024
    public void Monthly_GeneratesOnlyForCorrectWeekOfMonth(int year, int month, int day, MonthlyWeek which)
    {
        var start = new DateTime(2024, 6, 3);
        var target = new DateTime(year, month, day);
        Assert.True(StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.Monthly, start, skippedWeeks: [], lastShippedWeek: null,
            weekOf: target, monthlyWeek: which));
    }

    [Fact]
    public void Monthly_DoesNotGenerateOnNonTargetWeek()
    {
        var start = new DateTime(2024, 6, 3);
        var secondMonday = new DateTime(2024, 6, 10);
        Assert.False(StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.Monthly, start, skippedWeeks: [], lastShippedWeek: null,
            weekOf: secondMonday, monthlyWeek: MonthlyWeek.First));
    }

    // ── OnRequest ─────────────────────────────────────────────────────────────

    [Fact]
    public void OnRequest_ReturnsTrue_WhenStatusIsActive()
    {
        // OnRequest orders participate in the normal generate-instances cycle when Active.
        // The caller (AdminEndpoints) sets the standing order back to Paused after generating.
        var start = new DateTime(2024, 6, 3);
        var weekOf = start.AddDays(7);
        Assert.True(StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.OnRequest, start, skippedWeeks: [], lastShippedWeek: null, weekOf: weekOf));
    }

    // ── Stopped ───────────────────────────────────────────────────────────────

    [Fact]
    public void Stopped_NeverGenerates()
    {
        var start = new DateTime(2024, 6, 3);
        Assert.False(StandingOrderScheduleService.ShouldGenerateForWeek(
            OrderFrequency.Stopped, start, skippedWeeks: [], lastShippedWeek: null, weekOf: start));
    }

    // ── Invoice label ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("MAN", "CHI-IL", OrderChannel.Insulated, 2024, 1,  "MAN-CHI-IL-2024-01")]
    [InlineData("MAN", "CHI-IL", OrderChannel.Insulated, 2023, 7,  "MAN-CHI-IL-2023-07")]
    [InlineData("MAN", "CHI-IL", OrderChannel.FedEx,     2024, 3,  "AMZ-MAN-CHI-IL-2024-03")]
    [InlineData("SUN", "DAL-TX", OrderChannel.FedEx,     2024, 12, "AMZ-SUN-DAL-TX-2024-12")]
    public void BuildInvoiceLabel_IsCorrect(string nameAbbr, string locAbbr,
        OrderChannel channel, int seasonYear, int seekNum, string expected)
    {
        var customerKey = $"{nameAbbr}-{locAbbr}";
        var label = InvoiceLabelService.BuildLabel(customerKey, channel, seasonYear, seekNum);
        Assert.Equal(expected, label);
    }
}
