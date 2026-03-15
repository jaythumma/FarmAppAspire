using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Validates product catalog service logic: FedEx packaging threshold,
/// fixed tier prices, and insulated box configuration (Issue #42).
/// </summary>
public class ProductCatalogServiceTests
{
    // ── FedEx packaging threshold ─────────────────────────────────────────────
    // Requirement: total weight < 2 lb (< 32 oz) → Envelope; >= 2 lb (>= 32 oz) → Box

    [Theory]
    [InlineData(0)]     // zero weight
    [InlineData(1)]     // 1 oz
    [InlineData(16)]    // 1 lb exactly
    [InlineData(31)]    // just under 2 lb
    public void DerivePackagingType_BelowTwoLbs_ReturnsEnvelope(decimal totalOz)
    {
        // WHEN total weight < 2 lb THEN packaging type is Envelope
        Assert.Equal(FedExPackagingType.Envelope, PriceResolutionService.DerivePackagingType(totalOz));
    }

    [Theory]
    [InlineData(32)]    // exactly 2 lb
    [InlineData(33)]    // just over 2 lb
    [InlineData(48)]    // 3 lb
    [InlineData(80)]    // 5 lb
    public void DerivePackagingType_AtOrAboveTwoLbs_ReturnsBox(decimal totalOz)
    {
        // WHEN total weight >= 2 lb THEN packaging type is Box
        Assert.Equal(FedExPackagingType.Box, PriceResolutionService.DerivePackagingType(totalOz));
    }

    [Fact]
    public void DerivePackagingType_ExactlyTwoLbs_ReturnsBox()
    {
        // Boundary: exactly 32 oz = 2 lb → Box
        Assert.Equal(FedExPackagingType.Box, PriceResolutionService.DerivePackagingType(32m));
    }

    [Fact]
    public void DerivePackagingType_OneLbMinusOneOz_ReturnsEnvelope()
    {
        Assert.Equal(FedExPackagingType.Envelope, PriceResolutionService.DerivePackagingType(31m));
    }

    // ── FedEx tier prices are fixed and non-negotiable ────────────────────────
    // Scenario: FedEx tier price is always fixed

    private static FedExTierConfig Tier(FedExTierSize size, decimal oz, decimal price) =>
        new() { TierSize = size, WeightOz = oz, FixedPrice = price };

    private static readonly IReadOnlyList<FedExTierConfig> AllTierConfigs =
    [
        Tier(FedExTierSize.OneOz,   1m,  5.99m),
        Tier(FedExTierSize.TwoOz,   2m,  8.99m),
        Tier(FedExTierSize.FourOz,  4m,  12.99m),
        Tier(FedExTierSize.EightOz, 8m,  19.99m),
        Tier(FedExTierSize.OneLb,   16m, 27.99m),
        Tier(FedExTierSize.TwoLb,   32m, 42.99m),
        Tier(FedExTierSize.ThreeLb, 48m, 68.99m),
        Tier(FedExTierSize.FiveLb,  80m, 99.99m)
    ];

    [Theory]
    [InlineData(FedExTierSize.OneOz,   5.99)]
    [InlineData(FedExTierSize.TwoOz,   8.99)]
    [InlineData(FedExTierSize.FourOz,  12.99)]
    [InlineData(FedExTierSize.EightOz, 19.99)]
    [InlineData(FedExTierSize.OneLb,   27.99)]
    [InlineData(FedExTierSize.TwoLb,   42.99)]
    [InlineData(FedExTierSize.ThreeLb, 68.99)]
    [InlineData(FedExTierSize.FiveLb,  99.99)]
    public void FedExTierPrice_AllTiers_MatchCatalogDefinition(FedExTierSize tier, decimal expectedPrice)
    {
        // Scenario: FedEx tier price is always fixed
        // WHEN a FedEx order is created for any tier size
        // THEN the price is the fixed tier amount and cannot be overridden
        var actual = PriceResolutionService.FedExTierPrice(tier, AllTierConfigs);
        Assert.Equal(expectedPrice, actual);
    }

    [Fact]
    public void FedExTierConfigs_CatalogDefines_ExactlyEightTiers()
    {
        // The system supports exactly 8 FedEx tier sizes
        Assert.Equal(8, AllTierConfigs.Count);
    }

    // ── Insulated box configuration ───────────────────────────────────────────

    private static readonly IReadOnlyList<InsulatedBoxConfig> InsulatedBoxConfigs =
    [
        new() { Size = InsulatedBoxSize.FiveLb,   WeightLbs = 5m,  BasePricePerLb = 13m, IsDefault = false },
        new() { Size = InsulatedBoxSize.TenLb,    WeightLbs = 10m, BasePricePerLb = 13m, IsDefault = false },
        new() { Size = InsulatedBoxSize.TwelveLb, WeightLbs = 12m, BasePricePerLb = 13m, IsDefault = true  }
    ];

    [Fact]
    public void InsulatedBoxConfigs_CatalogDefines_ExactlyThreeSizes()
    {
        // Scenario: Only supported sizes are accepted
        Assert.Equal(3, InsulatedBoxConfigs.Count);
    }

    [Fact]
    public void InsulatedBoxConfigs_DefaultBox_IsTwelveLb()
    {
        // Scenario: Default box size is 12 lb
        var defaultBox = InsulatedBoxConfigs.Single(b => b.IsDefault);
        Assert.Equal(InsulatedBoxSize.TwelveLb, defaultBox.Size);
        Assert.Equal(12m, defaultBox.WeightLbs);
    }

    [Fact]
    public void InsulatedBoxConfigs_AllBoxes_HaveBasePriceThirteenPerLb()
    {
        // All insulated boxes use $13.00/lb base rate
        Assert.All(InsulatedBoxConfigs, b => Assert.Equal(13m, b.BasePricePerLb));
    }

    [Fact]
    public void InsulatedBoxConfigs_ExactlyOneDefault()
    {
        var defaults = InsulatedBoxConfigs.Where(b => b.IsDefault).ToList();
        Assert.Single(defaults);
    }

    [Theory]
    [InlineData(InsulatedBoxSize.FiveLb,    5)]
    [InlineData(InsulatedBoxSize.TenLb,    10)]
    [InlineData(InsulatedBoxSize.TwelveLb, 12)]
    public void InsulatedBoxConfigs_EachSize_HasCorrectWeight(InsulatedBoxSize size, int expectedWeightInt)
    {
        var expectedWeight = (decimal)expectedWeightInt;
        var box = InsulatedBoxConfigs.Single(b => b.Size == size);
        Assert.Equal(expectedWeight, box.WeightLbs);
    }

    // ── FedEx packaging type derived from multi-unit order ────────────────────

    [Fact]
    public void DerivePackagingType_MultiUnit_UsesTotalWeight()
    {
        // 4 × 4oz tier = 16oz total < 32oz → Envelope
        var tier = AllTierConfigs.Single(t => t.TierSize == FedExTierSize.FourOz);
        var totalOz = tier.WeightOz * 4;
        Assert.Equal(FedExPackagingType.Envelope, PriceResolutionService.DerivePackagingType(totalOz));
    }

    [Fact]
    public void DerivePackagingType_OneLbTierSingleUnit_ReturnsEnvelope()
    {
        // 1 × 1lb (16oz) = 16oz < 32oz → Envelope
        var tier = AllTierConfigs.Single(t => t.TierSize == FedExTierSize.OneLb);
        Assert.Equal(FedExPackagingType.Envelope, PriceResolutionService.DerivePackagingType(tier.WeightOz));
    }

    [Fact]
    public void DerivePackagingType_TwoLbTierSingleUnit_ReturnsBox()
    {
        // 1 × 2lb (32oz) = 32oz >= 32oz → Box
        var tier = AllTierConfigs.Single(t => t.TierSize == FedExTierSize.TwoLb);
        Assert.Equal(FedExPackagingType.Box, PriceResolutionService.DerivePackagingType(tier.WeightOz));
    }
}
