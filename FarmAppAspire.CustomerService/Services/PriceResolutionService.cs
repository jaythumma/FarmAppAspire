using FarmAppAspire.CustomerService.Models;

namespace FarmAppAspire.CustomerService.Services;

public static class PriceResolutionService
{
    /// <summary>
    /// Resolves the effective price per lb for an insulated order line.
    /// Priority: customer override (matching box size + min qty) → default $13/lb.
    /// </summary>
    public static decimal ResolveEffectivePricePerLb(
        InsulatedBoxSize boxSize,
        int qty,
        IEnumerable<CustomerPricing> overrides)
    {
        var match = overrides
            .FirstOrDefault(p => p.BoxSize == boxSize && qty >= p.MinQty);
        return match?.PricePerLb ?? 13m;
    }

    /// <summary>Computes aggregated totals for a standing order's lines.</summary>
    public static (int TotalBoxes, decimal TotalWeightLbs, decimal TotalAmount) ComputeStandingOrderTotals(
        IEnumerable<StandingOrderLine> lines,
        IEnumerable<InsulatedBoxConfig> boxConfigs,
        IEnumerable<CustomerPricing> overrides)
    {
        var configMap = boxConfigs.ToDictionary(c => c.Size);
        var lineList  = lines.ToList();

        var totalBoxes  = lineList.Sum(l => l.Qty);
        var totalWeight = lineList.Sum(l => configMap.TryGetValue(l.BoxSize, out var c) ? c.WeightLbs * l.Qty : 0);
        var totalAmount = lineList.Sum(l =>
        {
            if (!configMap.TryGetValue(l.BoxSize, out var c)) return 0m;
            var price = ResolveEffectivePricePerLb(l.BoxSize, l.Qty, overrides);
            return price * c.WeightLbs * l.Qty;
        });

        return (totalBoxes, totalWeight, totalAmount);
    }

    /// <summary>Computes aggregated totals for an order instance's lines.</summary>
    /// <remarks>
    /// FedEx lines use <c>FedExFixedPrice × Qty</c>.
    /// Insulated lines use <c>EffectivePricePerLb × WeightLbs × Qty</c> (weight from <paramref name="boxConfigs"/>).
    /// </remarks>
    public static (int TotalQty, decimal TotalAmount) ComputeOrderInstanceTotals(
        IEnumerable<OrderInstanceLine> lines,
        IEnumerable<InsulatedBoxConfig> boxConfigs)
    {
        var configMap = boxConfigs.ToDictionary(c => c.Size);
        var totalQty    = 0;
        var totalAmount = 0m;

        foreach (var line in lines)
        {
            totalQty += line.Qty;
            if (line.FedExFixedPrice.HasValue)
                totalAmount += line.FedExFixedPrice.Value * line.Qty;
            else if (line.EffectivePricePerLb.HasValue && line.BoxSize.HasValue
                     && configMap.TryGetValue(line.BoxSize.Value, out var config))
                totalAmount += line.EffectivePricePerLb.Value * config.WeightLbs * line.Qty;
        }

        return (totalQty, totalAmount);
    }

    /// <summary>Returns the fixed price for a FedEx tier (non-negotiable).</summary>
    public static decimal FedExTierPrice(FedExTierSize tier, IEnumerable<FedExTierConfig> configs) =>
        configs.First(c => c.TierSize == tier).FixedPrice;

    /// <summary>Derives FedEx packaging type from total order weight in oz.</summary>
    public static FedExPackagingType DerivePackagingType(decimal totalWeightOz) =>
        totalWeightOz < 32m ? FedExPackagingType.Envelope : FedExPackagingType.Box;
}
