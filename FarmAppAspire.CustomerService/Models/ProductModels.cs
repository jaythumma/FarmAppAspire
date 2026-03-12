namespace FarmAppAspire.CustomerService.Models;

/// <summary>Static seed data for insulated box sizes and their base price per lb.</summary>
public class InsulatedBoxConfig
{
    public InsulatedBoxSize Size { get; set; }
    public decimal WeightLbs { get; set; }
    public decimal BasePricePerLb { get; set; } = 13.00m;
    public bool IsDefault { get; set; }
}

/// <summary>Static seed data for FedEx tier sizes and their fixed prices.</summary>
public class FedExTierConfig
{
    public FedExTierSize TierSize { get; set; }
    /// <summary>Weight in ounces (internal storage unit).</summary>
    public decimal WeightOz { get; set; }
    public decimal FixedPrice { get; set; }
}
