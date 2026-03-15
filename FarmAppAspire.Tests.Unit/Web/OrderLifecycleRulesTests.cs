using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Validates <see cref="OrderLifecycleRules"/> order instance lifecycle rules (Issue #44).
/// </summary>
public class OrderLifecycleRulesTests
{
    // ── CanHarvest ────────────────────────────────────────────────────────────

    [Fact]
    public void CanHarvest_PendingStatus_ReturnsTrue()
    {
        Assert.True(OrderLifecycleRules.CanHarvest("Pending"));
    }

    [Theory]
    [InlineData("Harvested")]
    [InlineData("Inspected")]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void CanHarvest_NonPendingStatus_ReturnsFalse(string status)
    {
        Assert.False(OrderLifecycleRules.CanHarvest(status));
    }

    // ── CanInspect ────────────────────────────────────────────────────────────

    [Fact]
    public void CanInspect_HarvestedStatus_ReturnsTrue()
    {
        Assert.True(OrderLifecycleRules.CanInspect("Harvested"));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Inspected")]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void CanInspect_NonHarvestedStatus_ReturnsFalse(string status)
    {
        Assert.False(OrderLifecycleRules.CanInspect(status));
    }

    // ── CanShip ───────────────────────────────────────────────────────────────

    [Fact]
    public void CanShip_InspectedStatus_ReturnsTrue()
    {
        Assert.True(OrderLifecycleRules.CanShip("Inspected"));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Harvested")]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void CanShip_NonInspectedStatus_ReturnsFalse(string status)
    {
        Assert.False(OrderLifecycleRules.CanShip(status));
    }

    // ── CanCancel ─────────────────────────────────────────────────────────────

    [Fact]
    public void CanCancel_PendingStatus_ReturnsTrue()
    {
        Assert.True(OrderLifecycleRules.CanCancel("Pending"));
    }

    [Theory]
    [InlineData("Harvested")]
    [InlineData("Inspected")]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void CanCancel_NonPendingStatus_ReturnsFalse(string status)
    {
        Assert.False(OrderLifecycleRules.CanCancel(status));
    }

    // ── IsTerminal ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void IsTerminal_TerminalStatuses_ReturnsTrue(string status)
    {
        Assert.True(OrderLifecycleRules.IsTerminal(status));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Harvested")]
    [InlineData("Inspected")]
    public void IsTerminal_NonTerminalStatuses_ReturnsFalse(string status)
    {
        Assert.False(OrderLifecycleRules.IsTerminal(status));
    }

    // ── NextActionLabel ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("Pending",   "Harvest")]
    [InlineData("Harvested", "Inspect")]
    [InlineData("Inspected", "Ship")]
    public void NextActionLabel_ForwardStatuses_ReturnsExpectedLabel(string status, string expected)
    {
        Assert.Equal(expected, OrderLifecycleRules.NextActionLabel(status));
    }

    [Theory]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void NextActionLabel_TerminalStatuses_ReturnsNull(string status)
    {
        Assert.Null(OrderLifecycleRules.NextActionLabel(status));
    }

    // ── StatusBadgeClass ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Pending",   "bg-secondary")]
    [InlineData("Harvested", "bg-info text-dark")]
    [InlineData("Inspected", "bg-primary")]
    [InlineData("Shipped",   "bg-success")]
    [InlineData("Cancelled", "bg-danger")]
    public void StatusBadgeClass_KnownStatuses_ReturnExpectedClass(string status, string expected)
    {
        Assert.Equal(expected, OrderLifecycleRules.StatusBadgeClass(status));
    }

    [Fact]
    public void StatusBadgeClass_UnknownStatus_ReturnsFallback()
    {
        Assert.Equal("bg-light text-dark", OrderLifecycleRules.StatusBadgeClass("Unknown"));
    }

    // ── Lifecycle sequence invariants ─────────────────────────────────────────

    [Fact]
    public void Lifecycle_OnlyPendingCanHarvestAndCancel()
    {
        // Ensures exclusive access: only Pending allows both Harvest and Cancel
        var statuses = new[] { "Pending", "Harvested", "Inspected", "Shipped", "Cancelled" };
        var harvestable = statuses.Where(OrderLifecycleRules.CanHarvest).ToList();
        var cancellable = statuses.Where(OrderLifecycleRules.CanCancel).ToList();
        Assert.Equal(["Pending"], harvestable);
        Assert.Equal(["Pending"], cancellable);
    }

    [Fact]
    public void Lifecycle_ForwardTransitions_AreSequential()
    {
        // Pending → Harvested → Inspected → Shipped: each step unlocks exactly one action
        Assert.True(OrderLifecycleRules.CanHarvest("Pending"));
        Assert.False(OrderLifecycleRules.CanInspect("Pending"));
        Assert.False(OrderLifecycleRules.CanShip("Pending"));

        Assert.False(OrderLifecycleRules.CanHarvest("Harvested"));
        Assert.True(OrderLifecycleRules.CanInspect("Harvested"));
        Assert.False(OrderLifecycleRules.CanShip("Harvested"));

        Assert.False(OrderLifecycleRules.CanHarvest("Inspected"));
        Assert.False(OrderLifecycleRules.CanInspect("Inspected"));
        Assert.True(OrderLifecycleRules.CanShip("Inspected"));
    }
}
