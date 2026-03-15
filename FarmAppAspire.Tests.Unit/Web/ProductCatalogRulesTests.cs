using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Validates <see cref="ProductCatalogRules"/> catalog business rules (Issue #42).
/// </summary>
public class ProductCatalogRulesTests
{
    // ── ProductName ───────────────────────────────────────────────────────────

    [Fact]
    public void ProductName_IsCurryLeaf()
    {
        // Scenario: CurryLeaf is the only product
        Assert.Equal("CurryLeaf", ProductCatalogRules.ProductName);
    }

    // ── Channels are non-interchangeable ──────────────────────────────────────

    [Fact]
    public void ChannelsAreInterchangeable_ReturnsFalse()
    {
        // Scenario: Channels are non-interchangeable
        // WHEN a customer has a FedEx channel order
        // THEN the system SHALL NOT allow switching it to insulated pricing or box sizes
        Assert.False(ProductCatalogRules.ChannelsAreInterchangeable());
    }

    // ── Catalog size constants ─────────────────────────────────────────────────

    [Fact]
    public void SupportedInsulatedBoxCount_IsThree()
    {
        // Scenario: Only supported sizes are accepted — exactly 3 insulated sizes exist
        Assert.Equal(3, ProductCatalogRules.SupportedInsulatedBoxCount);
    }

    [Fact]
    public void SupportedFedExTierCount_IsEight()
    {
        // 8 FedEx tier sizes exist
        Assert.Equal(8, ProductCatalogRules.SupportedFedExTierCount);
    }

    // ── InsulatedBasePricePerLb ───────────────────────────────────────────────

    [Fact]
    public void InsulatedBasePricePerLb_Is13()
    {
        // Scenario: Base pricing applied per pound
        Assert.Equal(13.00m, ProductCatalogRules.InsulatedBasePricePerLb);
    }

    // ── InsulatedLineTotal ────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 5,  65.00)]   // 1 × 5 lb × $13 = $65
    [InlineData(1, 10, 130.00)]  // 1 × 10 lb × $13 = $130
    [InlineData(1, 12, 156.00)]  // 1 × 12 lb × $13 = $156  (default box)
    [InlineData(3, 10, 390.00)]  // 3 × 10 lb × $13 = $390
    [InlineData(5, 5,  325.00)]  // 5 × 5 lb × $13 = $325
    public void InsulatedLineTotal_BaseRate_ReturnsQtyTimesWeightTimesPrice(
        int qty, decimal weightLbs, decimal expected)
    {
        // Scenario: Base pricing applied per pound
        // line total = N × S × $13.00
        var total = ProductCatalogRules.InsulatedLineTotal(qty, weightLbs);
        Assert.Equal(expected, total);
    }

    [Fact]
    public void InsulatedLineTotal_ZeroQty_ReturnsZero()
    {
        Assert.Equal(0m, ProductCatalogRules.InsulatedLineTotal(0, 12m));
    }

    // ── IsDefaultInsulatedBox ─────────────────────────────────────────────────

    [Fact]
    public void IsDefaultInsulatedBox_WhenIsDefaultTrue_ReturnsTrue()
    {
        // Scenario: Default box size is 12 lb
        var defaultBox = new InsulatedBoxInfo("TwelveLb", 12m, 13.00m, IsDefault: true);
        Assert.True(ProductCatalogRules.IsDefaultInsulatedBox(defaultBox));
    }

    [Fact]
    public void IsDefaultInsulatedBox_WhenIsDefaultFalse_ReturnsFalse()
    {
        var nonDefaultBox = new InsulatedBoxInfo("TenLb", 10m, 13.00m, IsDefault: false);
        Assert.False(ProductCatalogRules.IsDefaultInsulatedBox(nonDefaultBox));
    }

    [Fact]
    public void IsDefaultInsulatedBox_FiveLbBox_ReturnsFalse()
    {
        var fiveLbBox = new InsulatedBoxInfo("FiveLb", 5m, 13.00m, IsDefault: false);
        Assert.False(ProductCatalogRules.IsDefaultInsulatedBox(fiveLbBox));
    }
}
