namespace FarmAppAspire.Web;

/// <summary>
/// Stateless helpers for filtering, sorting, and paginating the standing orders list.
/// Extracted from the Razor component so that logic can be unit-tested independently.
/// </summary>
public static class StandingOrderListHelper
{
    // ── Default sort ──────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the default sort: StartWeek descending, then Direct channel
    /// (Wholesale first, then Retail) before Amazon.
    /// </summary>
    public static AllStandingOrderSummary[] ApplyDefaultSort(AllStandingOrderSummary[] orders) =>
        orders
            .OrderByDescending(o => o.StartWeek)
            .ThenBy(o => o.CustomerChannelType == ChannelType.Direct ? 0 : 1)
            .ThenBy(o => o.CustomerType == CustomerType.Wholesale ? 0 : 1)
            .ToArray();

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Filters the list by an optional customer ID and an optional search text.
    /// </summary>
    public static AllStandingOrderSummary[] ApplyFilter(
        AllStandingOrderSummary[] orders,
        string searchText,
        string? customerIdFilter = null)
    {
        var result = orders.AsEnumerable();

        if (!string.IsNullOrEmpty(customerIdFilter) &&
            Guid.TryParse(customerIdFilter, out var customerId))
            result = result.Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var text = searchText.Trim();
            result = result.Where(o =>
                o.CustomerDisplayName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                o.Status.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                o.Frequency.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        return result.ToArray();
    }

    // ── Sorting ───────────────────────────────────────────────────────────────

    /// <summary>Sort by a named column.</summary>
    public static AllStandingOrderSummary[] ApplySort(
        AllStandingOrderSummary[] orders, string sortColumn, bool sortAscending)
    {
        IEnumerable<AllStandingOrderSummary> sorted = sortColumn switch
        {
            "Customer" => sortAscending
                ? orders.OrderBy(o => o.CustomerDisplayName, StringComparer.OrdinalIgnoreCase)
                : orders.OrderByDescending(o => o.CustomerDisplayName, StringComparer.OrdinalIgnoreCase),
            "Status" => sortAscending
                ? orders.OrderBy(o => o.Status)
                : orders.OrderByDescending(o => o.Status),
            "Frequency" => sortAscending
                ? orders.OrderBy(o => o.Frequency)
                : orders.OrderByDescending(o => o.Frequency),
            "StartWeek" => sortAscending
                ? orders.OrderBy(o => o.StartWeek)
                : orders.OrderByDescending(o => o.StartWeek),
            "Boxes" => sortAscending
                ? orders.OrderBy(o => o.TotalBoxes)
                : orders.OrderByDescending(o => o.TotalBoxes),
            "Weight" => sortAscending
                ? orders.OrderBy(o => o.TotalWeightLbs)
                : orders.OrderByDescending(o => o.TotalWeightLbs),
            "Amount" => sortAscending
                ? orders.OrderBy(o => o.TotalAmount)
                : orders.OrderByDescending(o => o.TotalAmount),
            _ => orders
        };
        return sorted.ToArray();
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    public static AllStandingOrderSummary[] ApplyPage(
        AllStandingOrderSummary[] orders, int page, int pageSize) =>
        orders.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

    public static int TotalPages(int totalItems, int pageSize) =>
        Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
}
