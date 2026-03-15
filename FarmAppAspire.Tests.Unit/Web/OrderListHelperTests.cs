using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

public class OrderListHelperTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AllOrderSummary Make(
        string customerName,
        string channelType,
        string customerType,
        string channel = "Insulated",
        string status = "Pending",
        DateTime? weekOf = null,
        int qty = 1,
        decimal amount = 100m) =>
        new(
            Guid.NewGuid(), null, Guid.NewGuid(),
            customerName, channelType, customerType,
            channel, status,
            weekOf ?? DateTime.Today,
            false, qty, amount);

    // ── ApplySort – WeekOf (default sort) ─────────────────────────────────────

    [Fact]
    public void ApplySort_WeekOf_Descending_MostRecentFirst()
    {
        var older = Make("A", "Direct", "Retail", weekOf: DateTime.Today.AddDays(-7));
        var newer = Make("B", "Direct", "Retail", weekOf: DateTime.Today);

        var result = OrderListHelper.ApplySort([older, newer], "WeekOf", false);

        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public void ApplySort_WeekOf_Ascending_OldestFirst()
    {
        var older = Make("A", "Direct", "Retail", weekOf: DateTime.Today.AddDays(-7));
        var newer = Make("B", "Direct", "Retail", weekOf: DateTime.Today);

        var result = OrderListHelper.ApplySort([older, newer], "WeekOf", true);

        Assert.Equal(older.Id, result[0].Id);
        Assert.Equal(newer.Id, result[1].Id);
    }

    [Fact]
    public void ApplySort_WeekOf_Descending_SameDateDirectBeforeAmazon()
    {
        var sameDate = DateTime.Today;
        var amazon  = Make("Amazon Customer", "Amazon",  "Retail",     weekOf: sameDate);
        var direct  = Make("Direct Customer", "Direct",  "Retail",     weekOf: sameDate);

        var result = OrderListHelper.ApplySort([amazon, direct], "WeekOf", false);

        Assert.Equal(direct.Id, result[0].Id);
        Assert.Equal(amazon.Id, result[1].Id);
    }

    [Fact]
    public void ApplySort_WeekOf_SameDateAndChannel_WholesaleBeforeRetail()
    {
        var sameDate = DateTime.Today;
        var retail    = Make("Retail C", "Direct", "Retail",    weekOf: sameDate);
        var wholesale = Make("WS C",     "Direct", "Wholesale", weekOf: sameDate);

        var result = OrderListHelper.ApplySort([retail, wholesale], "WeekOf", false);

        Assert.Equal(wholesale.Id, result[0].Id);
        Assert.Equal(retail.Id,    result[1].Id);
    }

    [Fact]
    public void ApplySort_WeekOf_MultipleFactors_CorrectOrder()
    {
        var week1 = DateTime.Today;
        var week2 = DateTime.Today.AddDays(-7);

        // week1 orders (newest)
        var w1DirectWholesale = Make("W1 WS",  "Direct",  "Wholesale", weekOf: week1);
        var w1DirectRetail    = Make("W1 Ret",  "Direct",  "Retail",    weekOf: week1);
        var w1Amazon          = Make("W1 AMZ",  "Amazon",  "Retail",    weekOf: week1);
        // week2 orders (older)
        var w2Direct          = Make("W2 Dir",  "Direct",  "Retail",    weekOf: week2);

        var result = OrderListHelper.ApplySort(
            [w2Direct, w1Amazon, w1DirectRetail, w1DirectWholesale], "WeekOf", false);

        Assert.Equal(w1DirectWholesale.Id, result[0].Id);
        Assert.Equal(w1DirectRetail.Id,    result[1].Id);
        Assert.Equal(w1Amazon.Id,          result[2].Id);
        Assert.Equal(w2Direct.Id,          result[3].Id);
    }

    // ── ApplySort – Customer ──────────────────────────────────────────────────

    [Fact]
    public void ApplySort_Customer_Ascending_AlphaOrder()
    {
        var z = Make("Zeta Farm", "Direct", "Retail");
        var a = Make("Alpha Farm", "Direct", "Retail");

        var result = OrderListHelper.ApplySort([z, a], "Customer", true);

        Assert.Equal("Alpha Farm", result[0].CustomerDisplayName);
        Assert.Equal("Zeta Farm",  result[1].CustomerDisplayName);
    }

    [Fact]
    public void ApplySort_Customer_Descending_ReverseAlpha()
    {
        var z = Make("Zeta Farm",  "Direct", "Retail");
        var a = Make("Alpha Farm", "Direct", "Retail");

        var result = OrderListHelper.ApplySort([a, z], "Customer", false);

        Assert.Equal("Zeta Farm",  result[0].CustomerDisplayName);
        Assert.Equal("Alpha Farm", result[1].CustomerDisplayName);
    }

    // ── ApplySort – Status ────────────────────────────────────────────────────

    [Fact]
    public void ApplySort_Status_Ascending_AlphaOrder()
    {
        var shipped  = Make("A", "Direct", "Retail", status: "Shipped");
        var pending  = Make("B", "Direct", "Retail", status: "Pending");

        var result = OrderListHelper.ApplySort([shipped, pending], "Status", true);

        Assert.Equal("Pending", result[0].Status);
        Assert.Equal("Shipped", result[1].Status);
    }

    // ── ApplySort – Channel ────────────────────────────────────────────────────

    [Fact]
    public void ApplySort_Channel_Ascending()
    {
        var insulated = Make("A", "Direct", "Retail", channel: "Insulated");
        var fedex     = Make("B", "Amazon", "Retail", channel: "FedEx");

        var result = OrderListHelper.ApplySort([fedex, insulated], "Channel", true);

        Assert.Equal("FedEx",     result[0].Channel);
        Assert.Equal("Insulated", result[1].Channel);
    }

    // ── ApplySort – TotalQty ──────────────────────────────────────────────────

    [Fact]
    public void ApplySort_TotalQty_Ascending()
    {
        var five = Make("A", "Direct", "Retail", qty: 5);
        var two  = Make("B", "Direct", "Retail", qty: 2);

        var result = OrderListHelper.ApplySort([five, two], "TotalQty", true);

        Assert.Equal(2, result[0].TotalQty);
        Assert.Equal(5, result[1].TotalQty);
    }

    [Fact]
    public void ApplySort_TotalQty_Descending()
    {
        var five = Make("A", "Direct", "Retail", qty: 5);
        var two  = Make("B", "Direct", "Retail", qty: 2);

        var result = OrderListHelper.ApplySort([five, two], "TotalQty", false);

        Assert.Equal(5, result[0].TotalQty);
        Assert.Equal(2, result[1].TotalQty);
    }

    // ── ApplySort – TotalAmount ───────────────────────────────────────────────

    [Fact]
    public void ApplySort_TotalAmount_Ascending()
    {
        var high = Make("A", "Direct", "Retail", amount: 500m);
        var low  = Make("B", "Direct", "Retail", amount: 100m);

        var result = OrderListHelper.ApplySort([high, low], "TotalAmount", true);

        Assert.Equal(100m, result[0].TotalAmount);
        Assert.Equal(500m, result[1].TotalAmount);
    }

    // ── ApplySort – Unknown column ────────────────────────────────────────────

    [Fact]
    public void ApplySort_UnknownColumn_AppliesDefaultSort()
    {
        var sameDate = DateTime.Today;
        var amazon  = Make("AMZ", "Amazon",  "Retail",    weekOf: sameDate);
        var direct  = Make("DIR", "Direct",  "Wholesale", weekOf: sameDate);

        // Unknown column should apply default sort: Direct before Amazon
        var result = OrderListHelper.ApplySort([amazon, direct], "UnknownColumn", true);

        Assert.Equal(direct.Id, result[0].Id);
        Assert.Equal(amazon.Id, result[1].Id);
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
        Assert.Equal(expected, OrderListHelper.TotalPages(items, pageSize));
    }

    // ── ApplyPage ─────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyPage_Page1_ReturnsFirstSlice()
    {
        var orders = Enumerable.Range(1, 10)
            .Select(i => Make($"C{i}", "Direct", "Retail"))
            .ToArray();

        var page = OrderListHelper.ApplyPage(orders, 1, 3);

        Assert.Equal(3, page.Length);
        Assert.Equal("C1", page[0].CustomerDisplayName);
    }

    [Fact]
    public void ApplyPage_LastPage_ReturnsRemainder()
    {
        var orders = Enumerable.Range(1, 10)
            .Select(i => Make($"C{i}", "Direct", "Retail"))
            .ToArray();

        var page = OrderListHelper.ApplyPage(orders, 4, 3);

        Assert.Single(page);
        Assert.Equal("C10", page[0].CustomerDisplayName);
    }

    [Fact]
    public void ApplyPage_EmptyList_ReturnsEmpty()
    {
        var page = OrderListHelper.ApplyPage([], 1, 25);

        Assert.Empty(page);
    }
}
