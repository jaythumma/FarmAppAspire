namespace FarmAppAspire.Web;

/// <summary>
/// UI-side business rules for order instance lifecycle transitions (Issue #44).
/// </summary>
public static class OrderLifecycleRules
{
    /// <summary>Returns <c>true</c> when the instance can be moved to Harvested.</summary>
    public static bool CanHarvest(string status) => status == "Pending";

    /// <summary>Returns <c>true</c> when the instance can be moved to Inspected.</summary>
    public static bool CanInspect(string status) => status == "Harvested";

    /// <summary>Returns <c>true</c> when the instance can be moved to Shipped.</summary>
    public static bool CanShip(string status) => status == "Inspected";

    /// <summary>Returns <c>true</c> when the instance can be cancelled.</summary>
    public static bool CanCancel(string status) => status == "Pending";

    /// <summary>Returns <c>true</c> when the instance is in a terminal state.</summary>
    public static bool IsTerminal(string status) => status is "Shipped" or "Cancelled";

    /// <summary>
    /// Returns the label of the next forward action for a given status,
    /// or <c>null</c> when no forward action exists.
    /// </summary>
    public static string? NextActionLabel(string status) => status switch
    {
        "Pending"   => "Harvest",
        "Harvested" => "Inspect",
        "Inspected" => "Ship",
        _           => null
    };

    /// <summary>
    /// Returns a Bootstrap badge CSS class for the given order instance status.
    /// </summary>
    public static string StatusBadgeClass(string status) => status switch
    {
        "Pending"   => "bg-secondary",
        "Harvested" => "bg-info text-dark",
        "Inspected" => "bg-primary",
        "Shipped"   => "bg-success",
        "Cancelled" => "bg-danger",
        _           => "bg-light text-dark"
    };
}
