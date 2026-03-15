using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class PriceResolutionServiceTests
{
    private static InsulatedBoxConfig Box(InsulatedBoxSize size, decimal weightLbs) =>
        new() { Size = size, WeightLbs = weightLbs, BasePricePerLb = 13m };

    private static CustomerPricing Override(InsulatedBoxSize size, decimal pricePerLb,
        int minQty = 1, decimal? shipping = null) =>
        new() { Id = Guid.NewGuid(), CustomerId = Guid.NewGuid(), BoxSize = size,
                PricePerLb = pricePerLb, MinQty = minQty, ShippingRate = shipping };

    // ── Effective price per lb ─────────────────────────────────────────────────

    [Fact]
    public void ResolvePrice_UsesDefault_WhenNoOverride()
    {
        var result = PriceResolutionService.ResolveEffectivePricePerLb(
            InsulatedBoxSize.TwelveLb, qty: 5, overrides: []);
        Assert.Equal(13m, result);
    }

    [Fact]
    public void ResolvePrice_UsesOverride_WhenMatchingBoxSizeAndQtyMet()
    {
        var overrides = new[] { Override(InsulatedBoxSize.TwelveLb, 11.50m, minQty: 3) };
        var result = PriceResolutionService.ResolveEffectivePricePerLb(
            InsulatedBoxSize.TwelveLb, qty: 5, overrides);
        Assert.Equal(11.50m, result);
    }

    [Fact]
    public void ResolvePrice_UsesDefault_WhenQtyBelowMinQty()
    {
        var overrides = new[] { Override(InsulatedBoxSize.TwelveLb, 11.50m, minQty: 50) };
        var result = PriceResolutionService.ResolveEffectivePricePerLb(
            InsulatedBoxSize.TwelveLb, qty: 30, overrides);
        Assert.Equal(13m, result);
    }

    [Fact]
    public void ResolvePrice_UsesDefault_WhenBoxSizeNotInOverrides()
    {
        var overrides = new[] { Override(InsulatedBoxSize.FiveLb, 12m) };
        var result = PriceResolutionService.ResolveEffectivePricePerLb(
            InsulatedBoxSize.TwelveLb, qty: 10, overrides);
        Assert.Equal(13m, result);
    }

    [Fact]
    public void ResolvePrice_MultipleOverrides_EachBoxSizeIndependent()
    {
        var overrides = new[]
        {
            Override(InsulatedBoxSize.FiveLb, 12m),
            Override(InsulatedBoxSize.TwelveLb, 11m)
        };
        Assert.Equal(12m,  PriceResolutionService.ResolveEffectivePricePerLb(InsulatedBoxSize.FiveLb,   qty: 1, overrides));
        Assert.Equal(11m,  PriceResolutionService.ResolveEffectivePricePerLb(InsulatedBoxSize.TwelveLb, qty: 1, overrides));
        Assert.Equal(13m,  PriceResolutionService.ResolveEffectivePricePerLb(InsulatedBoxSize.TenLb,    qty: 1, overrides));
    }

    // ── Standing order aggregated totals ─────────────────────────────────────

    [Fact]
    public void ComputeTotals_MultiLine_IsCorrect()
    {
        var lines = new List<StandingOrderLine>
        {
            new() { BoxSize = InsulatedBoxSize.FiveLb,   Qty = 3 },
            new() { BoxSize = InsulatedBoxSize.TwelveLb, Qty = 10 }
        };
        var boxConfigs = new List<InsulatedBoxConfig>
        {
            Box(InsulatedBoxSize.FiveLb,   5m),
            Box(InsulatedBoxSize.TwelveLb, 12m)
        };
        var overrides = Array.Empty<CustomerPricing>();

        var totals = PriceResolutionService.ComputeStandingOrderTotals(lines, boxConfigs, overrides);

        Assert.Equal(13,    totals.TotalBoxes);
        Assert.Equal(135m,  totals.TotalWeightLbs); // (3×5)+(10×12)
        Assert.Equal(13m * (3 * 5 + 10 * 12), totals.TotalAmount); // all at base price
    }

    // ── Order instance aggregated totals ──────────────────────────────────────

    private static OrderInstanceLine InsulatedLine(InsulatedBoxSize size, int qty, decimal pricePerLb) =>
        new() { Id = Guid.NewGuid(), BoxSize = size, Qty = qty, EffectivePricePerLb = pricePerLb };

    private static OrderInstanceLine FedExLine(FedExTierSize tier, int qty, decimal fixedPrice) =>
        new() { Id = Guid.NewGuid(), FedExTierSize = tier, Qty = qty, FedExFixedPrice = fixedPrice };

    [Fact]
    public void ComputeOrderInstanceTotals_FedExOnly_SumsFixedPriceTimesQty()
    {
        var lines = new[]
        {
            FedExLine(FedExTierSize.TwoLb, 3, 25.00m),
            FedExLine(FedExTierSize.FiveLb, 2, 45.00m)
        };

        var (totalQty, totalAmount) = PriceResolutionService.ComputeOrderInstanceTotals(lines, []);

        Assert.Equal(5,      totalQty);
        Assert.Equal(165.00m, totalAmount); // (3×25) + (2×45)
    }

    [Fact]
    public void ComputeOrderInstanceTotals_InsulatedOnly_SumesPricePerLbTimesWeightTimesQty()
    {
        var lines = new[]
        {
            InsulatedLine(InsulatedBoxSize.FiveLb,   2, 13.00m),
            InsulatedLine(InsulatedBoxSize.TwelveLb, 5, 11.50m)
        };
        var boxConfigs = new[]
        {
            Box(InsulatedBoxSize.FiveLb,   5m),
            Box(InsulatedBoxSize.TwelveLb, 12m)
        };

        var (totalQty, totalAmount) = PriceResolutionService.ComputeOrderInstanceTotals(lines, boxConfigs);

        Assert.Equal(7,      totalQty);
        // (2 × 13.00 × 5) + (5 × 11.50 × 12) = 130 + 690 = 820
        Assert.Equal(820.00m, totalAmount);
    }

    [Fact]
    public void ComputeOrderInstanceTotals_EmptyLines_ReturnsZeros()
    {
        var (totalQty, totalAmount) = PriceResolutionService.ComputeOrderInstanceTotals([], []);

        Assert.Equal(0,  totalQty);
        Assert.Equal(0m, totalAmount);
    }

    [Fact]
    public void ComputeOrderInstanceTotals_InsulatedLine_MissingBoxConfig_ExcludesFromAmount()
    {
        var lines = new[]
        {
            InsulatedLine(InsulatedBoxSize.TwelveLb, 3, 13.00m)
        };
        // no box configs provided — amount should be 0 but qty should still count
        var (totalQty, totalAmount) = PriceResolutionService.ComputeOrderInstanceTotals(lines, []);

        Assert.Equal(3,  totalQty);
        Assert.Equal(0m, totalAmount);
    }
}
