using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

public class CustomerListHelperTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CustomerSummary Make(
        string displayName,
        ChannelType channel,
        CustomerType type,
        string? key = null,
        string? email = null,
        string? company = null) =>
        new(Guid.NewGuid(), type, channel, displayName, company, email, null,
            0, 0m, key);

    // ── ApplyFilter ───────────────────────────────────────────────────────────

    [Fact]
    public void ApplyFilter_EmptySearch_ReturnsAll()
    {
        var customers = new[]
        {
            Make("Alice Farm", ChannelType.Direct, CustomerType.Retail),
            Make("Bob Ranch",  ChannelType.Direct, CustomerType.Wholesale),
        };

        var result = CustomerListHelper.ApplyFilter(customers, "");

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void ApplyFilter_WhitespaceSearch_ReturnsAll()
    {
        var customers = new[]
        {
            Make("Alice Farm", ChannelType.Direct, CustomerType.Retail),
        };

        var result = CustomerListHelper.ApplyFilter(customers, "   ");

        Assert.Single(result);
    }

    [Fact]
    public void ApplyFilter_ByDisplayName_CaseInsensitive()
    {
        var customers = new[]
        {
            Make("Alice Farm", ChannelType.Direct, CustomerType.Retail),
            Make("Bob Ranch",  ChannelType.Direct, CustomerType.Wholesale),
        };

        var result = CustomerListHelper.ApplyFilter(customers, "ALICE");

        Assert.Single(result);
        Assert.Equal("Alice Farm", result[0].DisplayName);
    }

    [Fact]
    public void ApplyFilter_ByCustomerKey_ReturnsMatch()
    {
        var customers = new[]
        {
            Make("A", ChannelType.Direct, CustomerType.Wholesale, "DIR-001"),
            Make("B", ChannelType.Direct, CustomerType.Wholesale, "DIR-002"),
        };

        var result = CustomerListHelper.ApplyFilter(customers, "DIR-001");

        Assert.Single(result);
        Assert.Equal("DIR-001", result[0].CustomerKey);
    }

    [Fact]
    public void ApplyFilter_ByEmail_ReturnsMatch()
    {
        var customers = new[]
        {
            Make("A", ChannelType.Direct, CustomerType.Wholesale, null, "alice@farm.com"),
            Make("B", ChannelType.Direct, CustomerType.Wholesale, null, "bob@ranch.com"),
        };

        var result = CustomerListHelper.ApplyFilter(customers, "alice");

        Assert.Single(result);
    }

    [Fact]
    public void ApplyFilter_ByCompany_ReturnsMatch()
    {
        var customers = new[]
        {
            Make("A", ChannelType.Direct, CustomerType.Wholesale, null, null, "Acme Corp"),
            Make("B", ChannelType.Direct, CustomerType.Wholesale, null, null, "Beta LLC"),
        };

        var result = CustomerListHelper.ApplyFilter(customers, "acme");

        Assert.Single(result);
        Assert.Equal("Acme Corp", result[0].CompanyName);
    }

    [Fact]
    public void ApplyFilter_NoMatch_ReturnsEmpty()
    {
        var customers = new[]
        {
            Make("Alice Farm", ChannelType.Direct, CustomerType.Retail),
        };

        var result = CustomerListHelper.ApplyFilter(customers, "zzz-no-match");

        Assert.Empty(result);
    }

    // ── ApplySort ─────────────────────────────────────────────────────────────

    [Fact]
    public void ApplySort_CustomerKey_Ascending_NullsLast()
    {
        var customers = new[]
        {
            Make("C", ChannelType.Direct, CustomerType.Wholesale, "DIR-002"),
            Make("A", ChannelType.Direct, CustomerType.Wholesale, null),
            Make("B", ChannelType.Direct, CustomerType.Wholesale, "DIR-001"),
        };

        var result = CustomerListHelper.ApplySort(customers, "CustomerKey", true);

        Assert.Equal("DIR-001", result[0].CustomerKey);
        Assert.Equal("DIR-002", result[1].CustomerKey);
        Assert.Null(result[2].CustomerKey);
    }

    [Fact]
    public void ApplySort_CustomerKey_Descending_NullsFirst()
    {
        var customers = new[]
        {
            Make("C", ChannelType.Direct, CustomerType.Wholesale, "DIR-002"),
            Make("A", ChannelType.Direct, CustomerType.Wholesale, null),
            Make("B", ChannelType.Direct, CustomerType.Wholesale, "DIR-001"),
        };

        var result = CustomerListHelper.ApplySort(customers, "CustomerKey", false);

        Assert.Null(result[0].CustomerKey);
        Assert.Equal("DIR-002", result[1].CustomerKey);
        Assert.Equal("DIR-001", result[2].CustomerKey);
    }

    [Fact]
    public void ApplySort_DisplayName_Ascending()
    {
        var customers = new[]
        {
            Make("Zeta",  ChannelType.Direct, CustomerType.Retail),
            Make("Alpha", ChannelType.Direct, CustomerType.Retail),
        };

        var result = CustomerListHelper.ApplySort(customers, "DisplayName", true);

        Assert.Equal("Alpha", result[0].DisplayName);
        Assert.Equal("Zeta",  result[1].DisplayName);
    }

    [Fact]
    public void ApplySort_DisplayName_Descending()
    {
        var customers = new[]
        {
            Make("Alpha", ChannelType.Direct, CustomerType.Retail),
            Make("Zeta",  ChannelType.Direct, CustomerType.Retail),
        };

        var result = CustomerListHelper.ApplySort(customers, "DisplayName", false);

        Assert.Equal("Zeta",  result[0].DisplayName);
        Assert.Equal("Alpha", result[1].DisplayName);
    }

    [Fact]
    public void ApplySort_UnknownColumn_ReturnsOriginalOrder()
    {
        var a = Make("A", ChannelType.Direct, CustomerType.Retail);
        var b = Make("B", ChannelType.Direct, CustomerType.Retail);
        var customers = new[] { a, b };

        var result = CustomerListHelper.ApplySort(customers, "UnknownColumn", true);

        Assert.Equal(a.Id, result[0].Id);
        Assert.Equal(b.Id, result[1].Id);
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
        Assert.Equal(expected, CustomerListHelper.TotalPages(items, pageSize));
    }

    // ── ApplyPage ─────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyPage_Page1_ReturnsFirstSlice()
    {
        var customers = Enumerable.Range(1, 10)
            .Select(i => Make($"C{i}", ChannelType.Direct, CustomerType.Retail))
            .ToArray();

        var page = CustomerListHelper.ApplyPage(customers, 1, 3);

        Assert.Equal(3, page.Length);
        Assert.Equal("C1", page[0].DisplayName);
    }

    [Fact]
    public void ApplyPage_LastPage_ReturnsRemainder()
    {
        var customers = Enumerable.Range(1, 10)
            .Select(i => Make($"C{i}", ChannelType.Direct, CustomerType.Retail))
            .ToArray();

        var page = CustomerListHelper.ApplyPage(customers, 4, 3);

        Assert.Single(page);
        Assert.Equal("C10", page[0].DisplayName);
    }

    // ── GroupCustomers ────────────────────────────────────────────────────────

    [Fact]
    public void GroupCustomers_DirectFirst_AmazonSecond()
    {
        var customers = new[]
        {
            Make("Amazon WS", ChannelType.Amazon, CustomerType.Wholesale, "AMZN-001"),
            Make("Direct WS", ChannelType.Direct, CustomerType.Wholesale, "DIR-001"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "CustomerKey", true);

        Assert.Equal(2, groups.Count);
        Assert.Equal(ChannelType.Direct, groups[0].Channel);
        Assert.Equal(ChannelType.Amazon, groups[1].Channel);
    }

    [Fact]
    public void GroupCustomers_OnlyAmazon_ReturnsSingleGroup()
    {
        var customers = new[]
        {
            Make("Amazon WS", ChannelType.Amazon, CustomerType.Wholesale, "AMZN-001"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "CustomerKey", true);

        Assert.Single(groups);
        Assert.Equal(ChannelType.Amazon, groups[0].Channel);
    }

    [Fact]
    public void GroupCustomers_DirectHasWholesaleSubgroupFirst()
    {
        var customers = new[]
        {
            Make("Retail A",    ChannelType.Direct, CustomerType.Retail,    "RET-001"),
            Make("Wholesale A", ChannelType.Direct, CustomerType.Wholesale, "WS-001"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "CustomerKey", true);

        var directGroup = groups.Single(g => g.Channel == ChannelType.Direct);
        Assert.Equal(2, directGroup.TypeGroups.Count);
        Assert.Equal("Wholesale", directGroup.TypeGroups[0].TypeLabel);
        Assert.Equal("Retail",    directGroup.TypeGroups[1].TypeLabel);
    }

    [Fact]
    public void GroupCustomers_DirectOnlyWholesale_SingleSubgroup()
    {
        var customers = new[]
        {
            Make("WS A", ChannelType.Direct, CustomerType.Wholesale, "WS-001"),
            Make("WS B", ChannelType.Direct, CustomerType.Wholesale, "WS-002"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "CustomerKey", true);

        var directGroup = groups.Single(g => g.Channel == ChannelType.Direct);
        Assert.Single(directGroup.TypeGroups);
        Assert.Equal("Wholesale", directGroup.TypeGroups[0].TypeLabel);
    }

    [Fact]
    public void GroupCustomers_SortsAscendingByCustomerKeyWithinSubgroup()
    {
        var customers = new[]
        {
            Make("B WS", ChannelType.Direct, CustomerType.Wholesale, "DIR-002"),
            Make("A WS", ChannelType.Direct, CustomerType.Wholesale, "DIR-001"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "CustomerKey", true);
        var ws = groups.Single(g => g.Channel == ChannelType.Direct)
                       .TypeGroups.Single(tg => tg.TypeLabel == "Wholesale");

        Assert.Equal("DIR-001", ws.Customers[0].CustomerKey);
        Assert.Equal("DIR-002", ws.Customers[1].CustomerKey);
    }

    [Fact]
    public void GroupCustomers_AmazonHasNoTypeGroups()
    {
        var customers = new[]
        {
            Make("AMZN A", ChannelType.Amazon, CustomerType.Wholesale, "AMZN-001"),
            Make("AMZN B", ChannelType.Amazon, CustomerType.Retail,    "AMZN-002"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "CustomerKey", true);
        var amazonGroup = groups.Single(g => g.Channel == ChannelType.Amazon);

        Assert.Empty(amazonGroup.TypeGroups);
    }

    [Fact]
    public void GroupCustomers_TotalCount_ReflectsAllCustomersInChannel()
    {
        var customers = new[]
        {
            Make("WS A",  ChannelType.Direct, CustomerType.Wholesale, "DIR-001"),
            Make("WS B",  ChannelType.Direct, CustomerType.Wholesale, "DIR-002"),
            Make("Ret A", ChannelType.Direct, CustomerType.Retail,    "RET-001"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "CustomerKey", true);

        Assert.Equal(3, groups.Single(g => g.Channel == ChannelType.Direct).TotalCount);
    }

    [Fact]
    public void GroupCustomers_EmptyInput_ReturnsEmptyList()
    {
        var groups = CustomerListHelper.GroupCustomers([], "CustomerKey", true);

        Assert.Empty(groups);
    }

    [Fact]
    public void GroupCustomers_SortDescending_AppliedWithinGroups()
    {
        var customers = new[]
        {
            Make("Alpha", ChannelType.Direct, CustomerType.Wholesale, "DIR-001"),
            Make("Zeta",  ChannelType.Direct, CustomerType.Wholesale, "DIR-002"),
        };

        var groups = CustomerListHelper.GroupCustomers(customers, "DisplayName", false);
        var ws = groups.Single(g => g.Channel == ChannelType.Direct)
                       .TypeGroups.Single(tg => tg.TypeLabel == "Wholesale");

        Assert.Equal("Zeta",  ws.Customers[0].DisplayName);
        Assert.Equal("Alpha", ws.Customers[1].DisplayName);
    }
}
