using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

public class StandingOrderListHelperTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AllStandingOrderSummary Make(
        string customerName,
        ChannelType channel,
        CustomerType type,
        DateTime? startWeek = null,
        string status = "Active",
        string frequency = "Weekly") =>
        new(
            Guid.NewGuid(), Guid.NewGuid(), customerName,
            channel, type,
            status, frequency, null,
            false, 2025, startWeek ?? new DateTime(2025, 1, 6),
            1, 10m, 100m,
            DateTime.UtcNow, "test");

    // ── ApplyDefaultSort ──────────────────────────────────────────────────────

    [Fact]
    public void ApplyDefaultSort_NewerStartWeekFirst()
    {
        var older = Make("A", ChannelType.Direct, CustomerType.Wholesale, new DateTime(2025, 1, 6));
        var newer = Make("B", ChannelType.Direct, CustomerType.Wholesale, new DateTime(2025, 2, 3));

        var result = StandingOrderListHelper.ApplyDefaultSort([older, newer]);

        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public void ApplyDefaultSort_SameDate_DirectBeforeAmazon()
    {
        var week = new DateTime(2025, 1, 6);
        var amazon = Make("Amazon", ChannelType.Amazon, CustomerType.Retail,    week);
        var direct = Make("Direct", ChannelType.Direct, CustomerType.Wholesale, week);

        var result = StandingOrderListHelper.ApplyDefaultSort([amazon, direct]);

        Assert.Equal(direct.Id, result[0].Id);
        Assert.Equal(amazon.Id, result[1].Id);
    }

    [Fact]
    public void ApplyDefaultSort_SameDate_WithinDirect_WholesaleBeforeRetail()
    {
        var week   = new DateTime(2025, 1, 6);
        var retail = Make("Retail",    ChannelType.Direct, CustomerType.Retail,    week);
        var ws     = Make("Wholesale", ChannelType.Direct, CustomerType.Wholesale, week);

        var result = StandingOrderListHelper.ApplyDefaultSort([retail, ws]);

        Assert.Equal(ws.Id,     result[0].Id);
        Assert.Equal(retail.Id, result[1].Id);
    }

    [Fact]
    public void ApplyDefaultSort_FullHierarchy()
    {
        var week = new DateTime(2025, 1, 6);
        var amazon  = Make("Amazon",    ChannelType.Amazon, CustomerType.Retail,    week);
        var retail  = Make("Retail",    ChannelType.Direct, CustomerType.Retail,    week);
        var ws      = Make("Wholesale", ChannelType.Direct, CustomerType.Wholesale, week);
        var oldDirect = Make("Old",     ChannelType.Direct, CustomerType.Wholesale, new DateTime(2025, 1, 1));

        var result = StandingOrderListHelper.ApplyDefaultSort([amazon, retail, ws, oldDirect]);

        // newer date before older date
        Assert.NotEqual(oldDirect.Id, result[0].Id);
        Assert.NotEqual(oldDirect.Id, result[1].Id);
        Assert.NotEqual(oldDirect.Id, result[2].Id);
        Assert.Equal(oldDirect.Id, result[3].Id);
        // within same week: ws first, then retail, then amazon
        Assert.Equal(ws.Id,     result[0].Id);
        Assert.Equal(retail.Id, result[1].Id);
        Assert.Equal(amazon.Id, result[2].Id);
    }

    // ── ApplyFilter ───────────────────────────────────────────────────────────

    [Fact]
    public void ApplyFilter_EmptySearch_ReturnsAll()
    {
        var orders = new[]
        {
            Make("Alice Farm", ChannelType.Direct, CustomerType.Retail),
            Make("Bob Ranch",  ChannelType.Direct, CustomerType.Wholesale),
        };

        var result = StandingOrderListHelper.ApplyFilter(orders, "");

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void ApplyFilter_ByCustomerName_CaseInsensitive()
    {
        var orders = new[]
        {
            Make("Alice Farm", ChannelType.Direct, CustomerType.Retail),
            Make("Bob Ranch",  ChannelType.Direct, CustomerType.Wholesale),
        };

        var result = StandingOrderListHelper.ApplyFilter(orders, "ALICE");

        Assert.Single(result);
        Assert.Equal("Alice Farm", result[0].CustomerDisplayName);
    }

    [Fact]
    public void ApplyFilter_ByStatus_CaseInsensitive()
    {
        var orders = new[]
        {
            Make("A", ChannelType.Direct, CustomerType.Retail,    status: "Active"),
            Make("B", ChannelType.Direct, CustomerType.Wholesale, status: "Paused"),
        };

        var result = StandingOrderListHelper.ApplyFilter(orders, "paused");

        Assert.Single(result);
        Assert.Equal("Paused", result[0].Status);
    }

    [Fact]
    public void ApplyFilter_ByFrequency_CaseInsensitive()
    {
        var orders = new[]
        {
            Make("A", ChannelType.Direct, CustomerType.Retail,    frequency: "Weekly"),
            Make("B", ChannelType.Direct, CustomerType.Wholesale, frequency: "BiWeekly"),
        };

        var result = StandingOrderListHelper.ApplyFilter(orders, "biweekly");

        Assert.Single(result);
    }

    [Fact]
    public void ApplyFilter_NoMatch_ReturnsEmpty()
    {
        var orders = new[]
        {
            Make("Alice Farm", ChannelType.Direct, CustomerType.Retail),
        };

        var result = StandingOrderListHelper.ApplyFilter(orders, "zzz-no-match");

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyFilter_ByCustomerId_ReturnsOnlyThatCustomer()
    {
        var id = Guid.NewGuid();
        var match = new AllStandingOrderSummary(
            Guid.NewGuid(), id, "Alice", ChannelType.Direct, CustomerType.Wholesale,
            "Active", "Weekly", null, false, 2025, new DateTime(2025, 1, 6),
            1, 10m, 100m, DateTime.UtcNow, "test");
        var other = Make("Bob", ChannelType.Direct, CustomerType.Retail);

        var result = StandingOrderListHelper.ApplyFilter([match, other], "", id.ToString());

        Assert.Single(result);
        Assert.Equal(id, result[0].CustomerId);
    }

    [Fact]
    public void ApplyFilter_InvalidCustomerId_DoesNotFilter()
    {
        var orders = new[]
        {
            Make("Alice", ChannelType.Direct, CustomerType.Retail),
            Make("Bob",   ChannelType.Direct, CustomerType.Wholesale),
        };

        var result = StandingOrderListHelper.ApplyFilter(orders, "", "not-a-guid");

        Assert.Equal(2, result.Length);
    }

    // ── ApplySort ─────────────────────────────────────────────────────────────

    [Fact]
    public void ApplySort_Customer_Ascending()
    {
        var orders = new[]
        {
            Make("Zeta",  ChannelType.Direct, CustomerType.Retail),
            Make("Alpha", ChannelType.Direct, CustomerType.Retail),
        };

        var result = StandingOrderListHelper.ApplySort(orders, "Customer", true);

        Assert.Equal("Alpha", result[0].CustomerDisplayName);
        Assert.Equal("Zeta",  result[1].CustomerDisplayName);
    }

    [Fact]
    public void ApplySort_Customer_Descending()
    {
        var orders = new[]
        {
            Make("Alpha", ChannelType.Direct, CustomerType.Retail),
            Make("Zeta",  ChannelType.Direct, CustomerType.Retail),
        };

        var result = StandingOrderListHelper.ApplySort(orders, "Customer", false);

        Assert.Equal("Zeta",  result[0].CustomerDisplayName);
        Assert.Equal("Alpha", result[1].CustomerDisplayName);
    }

    [Fact]
    public void ApplySort_StartWeek_Ascending()
    {
        var older = Make("A", ChannelType.Direct, CustomerType.Wholesale, new DateTime(2025, 1, 6));
        var newer = Make("B", ChannelType.Direct, CustomerType.Wholesale, new DateTime(2025, 2, 3));

        var result = StandingOrderListHelper.ApplySort([newer, older], "StartWeek", true);

        Assert.Equal(older.Id, result[0].Id);
        Assert.Equal(newer.Id, result[1].Id);
    }

    [Fact]
    public void ApplySort_StartWeek_Descending()
    {
        var older = Make("A", ChannelType.Direct, CustomerType.Wholesale, new DateTime(2025, 1, 6));
        var newer = Make("B", ChannelType.Direct, CustomerType.Wholesale, new DateTime(2025, 2, 3));

        var result = StandingOrderListHelper.ApplySort([older, newer], "StartWeek", false);

        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public void ApplySort_UnknownColumn_ReturnsOriginalOrder()
    {
        var a = Make("A", ChannelType.Direct, CustomerType.Retail);
        var b = Make("B", ChannelType.Direct, CustomerType.Retail);

        var result = StandingOrderListHelper.ApplySort([a, b], "UnknownColumn", true);

        Assert.Equal(a.Id, result[0].Id);
        Assert.Equal(b.Id, result[1].Id);
    }

    [Theory]
    [InlineData("Status")]
    [InlineData("Frequency")]
    [InlineData("Boxes")]
    [InlineData("Weight")]
    [InlineData("Amount")]
    public void ApplySort_SupportedColumns_DoNotThrow(string column)
    {
        var orders = new[]
        {
            Make("A", ChannelType.Direct, CustomerType.Retail),
            Make("B", ChannelType.Direct, CustomerType.Wholesale),
        };

        var asc  = StandingOrderListHelper.ApplySort(orders, column, true);
        var desc = StandingOrderListHelper.ApplySort(orders, column, false);

        Assert.Equal(2, asc.Length);
        Assert.Equal(2, desc.Length);
    }

    // ── TotalPages ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0,   25, 1)]
    [InlineData(1,   25, 1)]
    [InlineData(25,  25, 1)]
    [InlineData(26,  25, 2)]
    [InlineData(100, 25, 4)]
    [InlineData(101, 25, 5)]
    public void TotalPages_ReturnsCorrectCount(int items, int pageSize, int expected)
    {
        Assert.Equal(expected, StandingOrderListHelper.TotalPages(items, pageSize));
    }

    // ── ApplyPage ─────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyPage_Page1_ReturnsFirstSlice()
    {
        var orders = Enumerable.Range(1, 10)
            .Select(i => Make($"C{i}", ChannelType.Direct, CustomerType.Retail))
            .ToArray();

        var page = StandingOrderListHelper.ApplyPage(orders, 1, 3);

        Assert.Equal(3, page.Length);
        Assert.Equal("C1", page[0].CustomerDisplayName);
    }

    [Fact]
    public void ApplyPage_LastPage_ReturnsRemainder()
    {
        var orders = Enumerable.Range(1, 10)
            .Select(i => Make($"C{i}", ChannelType.Direct, CustomerType.Retail))
            .ToArray();

        var page = StandingOrderListHelper.ApplyPage(orders, 4, 3);

        Assert.Single(page);
        Assert.Equal("C10", page[0].CustomerDisplayName);
    }

    [Fact]
    public void ApplyPage_BeyondLastPage_ReturnsEmpty()
    {
        var orders = Enumerable.Range(1, 5)
            .Select(i => Make($"C{i}", ChannelType.Direct, CustomerType.Retail))
            .ToArray();

        var page = StandingOrderListHelper.ApplyPage(orders, 10, 25);

        Assert.Empty(page);
    }
}
