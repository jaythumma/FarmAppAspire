namespace FarmAppAspire.Web;

/// <summary>
/// Stateless helpers for sorting and paginating the orders list.
/// Extracted from the Razor component so that logic can be unit-tested independently.
/// </summary>
public static class OrderListHelper
{
    // Channel sort priority: Direct first (0), Amazon last (1).
    // Any channel type that is not "Direct" is treated as Amazon-equivalent (last in sort order),
    // which is the correct fallback given the two supported values (Direct, Amazon).
    private static int ChannelSortOrder(string customerChannelType) =>
        customerChannelType == "Direct" ? 0 : 1;

    // Customer type sort priority within Direct channel: Wholesale first (0), Retail second (1).
    // Any customer type that is not "Wholesale" is treated as Retail-equivalent,
    // which is the correct fallback given the two supported values (Wholesale, Retail).
    private static int CustomerTypeSortOrder(string customerType) =>
        customerType == "Wholesale" ? 0 : 1;

    // ── Sorting ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sorts orders by the given column.
    /// When sorting by WeekOf (the default), applies the full default secondary sort:
    /// descending date → Direct channel (Wholesale, then Retail) → Amazon.
    /// </summary>
    public static AllOrderSummary[] ApplySort(AllOrderSummary[] orders, string sortColumn, bool sortAscending)
    {
        IEnumerable<AllOrderSummary> sorted = sortColumn switch
        {
            "Customer" => sortAscending
                ? orders.OrderBy(o => o.CustomerDisplayName, StringComparer.OrdinalIgnoreCase)
                : orders.OrderByDescending(o => o.CustomerDisplayName, StringComparer.OrdinalIgnoreCase),

            "WeekOf" => sortAscending
                ? orders.OrderBy(o => o.WeekOf)
                        .ThenBy(o => ChannelSortOrder(o.CustomerChannelType))
                        .ThenBy(o => CustomerTypeSortOrder(o.CustomerType))
                : orders.OrderByDescending(o => o.WeekOf)
                        .ThenBy(o => ChannelSortOrder(o.CustomerChannelType))
                        .ThenBy(o => CustomerTypeSortOrder(o.CustomerType)),

            "Channel" => sortAscending
                ? orders.OrderBy(o => o.Channel, StringComparer.OrdinalIgnoreCase)
                : orders.OrderByDescending(o => o.Channel, StringComparer.OrdinalIgnoreCase),

            "Status" => sortAscending
                ? orders.OrderBy(o => o.Status, StringComparer.OrdinalIgnoreCase)
                : orders.OrderByDescending(o => o.Status, StringComparer.OrdinalIgnoreCase),

            "TotalQty" => sortAscending
                ? orders.OrderBy(o => o.TotalQty)
                : orders.OrderByDescending(o => o.TotalQty),

            "TotalAmount" => sortAscending
                ? orders.OrderBy(o => o.TotalAmount)
                : orders.OrderByDescending(o => o.TotalAmount),

            // Unknown column: apply full default sort (newest first, Direct before Amazon, Wholesale before Retail).
            _ => orders.OrderByDescending(o => o.WeekOf)
                       .ThenBy(o => ChannelSortOrder(o.CustomerChannelType))
                       .ThenBy(o => CustomerTypeSortOrder(o.CustomerType))
        };

        return sorted.ToArray();
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    public static AllOrderSummary[] ApplyPage(AllOrderSummary[] orders, int page, int pageSize) =>
        orders.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

    public static int TotalPages(int totalItems, int pageSize) =>
        Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
}
