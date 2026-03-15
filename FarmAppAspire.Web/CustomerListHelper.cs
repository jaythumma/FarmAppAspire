namespace FarmAppAspire.Web;

/// <summary>
/// Stateless helpers for filtering, sorting, grouping, and paginating the customer list.
/// Extracted from the Razor component so that logic can be unit-tested independently.
/// </summary>
public static class CustomerListHelper
{
    // ── Filtering ─────────────────────────────────────────────────────────────

    public static CustomerSummary[] ApplyFilter(CustomerSummary[] customers, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return customers;
        var text = searchText.Trim();
        return customers.Where(c =>
            (c.DisplayName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.CustomerKey?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.PrimaryEmail?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.CompanyName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToArray();
    }

    // ── Sorting ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sort customers by column. Null CustomerKey values sort last when ascending,
    /// first when descending.
    /// </summary>
    public static CustomerSummary[] ApplySort(CustomerSummary[] customers, string sortColumn, bool sortAscending)
    {
        IEnumerable<CustomerSummary> sorted = sortColumn switch
        {
            "CustomerKey" => sortAscending
                ? customers.OrderBy(c => c.CustomerKey is null).ThenBy(c => c.CustomerKey)
                : customers.OrderByDescending(c => c.CustomerKey is null).ThenByDescending(c => c.CustomerKey),
            "DisplayName" => sortAscending
                ? customers.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                : customers.OrderByDescending(c => c.DisplayName, StringComparer.OrdinalIgnoreCase),
            "Type" => sortAscending
                ? customers.OrderBy(c => c.Type.ToString())
                : customers.OrderByDescending(c => c.Type.ToString()),
            "Company" => sortAscending
                ? customers.OrderBy(c => c.CompanyName, StringComparer.OrdinalIgnoreCase)
                : customers.OrderByDescending(c => c.CompanyName, StringComparer.OrdinalIgnoreCase),
            "Orders" => sortAscending
                ? customers.OrderBy(c => c.OrderCount)
                : customers.OrderByDescending(c => c.OrderCount),
            "Total" => sortAscending
                ? customers.OrderBy(c => c.TotalOrderAmount)
                : customers.OrderByDescending(c => c.TotalOrderAmount),
            _ => customers
        };
        return sorted.ToArray();
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    public static CustomerSummary[] ApplyPage(CustomerSummary[] customers, int page, int pageSize) =>
        customers.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

    public static int TotalPages(int totalItems, int pageSize) =>
        Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));

    // ── Grouping ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Groups customers into channel buckets (Direct first, Amazon second).
    /// Within Direct, customers are further sub-grouped by Wholesale then Retail.
    /// Each group is sorted according to the provided sort parameters.
    /// </summary>
    public static IReadOnlyList<CustomerChannelGroup> GroupCustomers(
        CustomerSummary[] customers, string sortColumn, bool sortAscending)
    {
        var groups = new List<CustomerChannelGroup>();

        foreach (var channel in new[] { ChannelType.Direct, ChannelType.Amazon })
        {
            var channelCustomers = ApplySort(
                customers.Where(c => c.ChannelType == channel).ToArray(),
                sortColumn, sortAscending);

            if (channelCustomers.Length == 0) continue;

            var typeGroups = new List<CustomerTypeGroup>();
            if (channel == ChannelType.Direct)
            {
                var wholesale = channelCustomers.Where(c => c.Type == CustomerType.Wholesale).ToList();
                var retail    = channelCustomers.Where(c => c.Type == CustomerType.Retail).ToList();
                if (wholesale.Count > 0) typeGroups.Add(new CustomerTypeGroup("Wholesale", wholesale));
                if (retail.Count    > 0) typeGroups.Add(new CustomerTypeGroup("Retail",    retail));
            }

            groups.Add(new CustomerChannelGroup(channel, channelCustomers.ToList(), typeGroups));
        }

        return groups;
    }
}

// ── Data transfer types ───────────────────────────────────────────────────────

public record CustomerTypeGroup(string TypeLabel, IReadOnlyList<CustomerSummary> Customers);

public record CustomerChannelGroup(
    ChannelType Channel,
    IReadOnlyList<CustomerSummary> AllCustomers,
    IReadOnlyList<CustomerTypeGroup> TypeGroups)
{
    public int TotalCount => AllCustomers.Count;
}
