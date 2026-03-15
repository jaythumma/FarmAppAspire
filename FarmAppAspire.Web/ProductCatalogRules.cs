namespace FarmAppAspire.Web;

/// <summary>
/// Business rules for the product catalog (Issue #42).
/// CurryLeaf is the single orderable product, sold through two mutually exclusive channels:
/// Insulated (direct farm shipment) and FedEx (Amazon/external channel).
/// </summary>
public static class ProductCatalogRules
{
    /// <summary>The single orderable product.</summary>
    public const string ProductName = "CurryLeaf";

    /// <summary>Base price per pound for all insulated box sizes.</summary>
    public const decimal InsulatedBasePricePerLb = 13.00m;

    /// <summary>Number of supported insulated box sizes.</summary>
    public const int SupportedInsulatedBoxCount = 3;

    /// <summary>Number of supported FedEx tier sizes.</summary>
    public const int SupportedFedExTierCount = 8;

    /// <summary>
    /// Calculates the insulated order line total at the base rate.
    /// Formula: qty × weightLbs × $13.00/lb.
    /// </summary>
    public static decimal InsulatedLineTotal(int qty, decimal weightLbs) =>
        qty * weightLbs * InsulatedBasePricePerLb;

    /// <summary>
    /// Returns <c>true</c> if the given box is the system default (12 lb).
    /// </summary>
    public static bool IsDefaultInsulatedBox(InsulatedBoxInfo box) => box.IsDefault;

    /// <summary>
    /// Returns <c>false</c>; Insulated and FedEx channels are mutually exclusive
    /// and SHALL NOT be substituted for one another.
    /// </summary>
    public static bool ChannelsAreInterchangeable() => false;
}
