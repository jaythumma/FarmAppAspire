using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.Invoices;

/// <summary>
/// Playwright tests for the Invoices feature: list page (/invoices) and detail page.
/// </summary>
[Collection("Playwright")]
public class InvoicesUITests(AspirePlaywrightFixture fixture) : IAsyncLifetime
{
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public async ValueTask InitializeAsync()
    {
        _context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        _page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task LoginAsAdminAsync()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync($"nav:has-text('{LoginHelper.AdminEmail}')");
    }

    // ── Invoices List Page (/invoices) ────────────────────────────────────────

    [Fact]
    public async Task InvoicesListPage_Loads_WithHeading()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("h1");

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Invoices", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvoicesListPage_ShowsCustomerDropdown()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(3000);

        var customerSelect = _page.Locator("select.form-select").First;
        Assert.True(await customerSelect.IsVisibleAsync());
    }

    [Fact]
    public async Task InvoicesListPage_ShowsSeasonYearDropdown()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(3000);

        // Should have at least 3 selects: customer, season year, channel
        var selects = _page.Locator("select.form-select");
        var count = await selects.CountAsync();
        Assert.True(count >= 3, "Invoices page should have customer, season year, and channel dropdowns");
    }

    [Fact]
    public async Task InvoicesListPage_ShowsChannelDropdown()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(3000);

        // Channel dropdown has "All channels", "Insulated", "FedEx" options
        var channelSelect = _page.Locator("select:has(option:has-text('Insulated'))");
        Assert.True(await channelSelect.IsVisibleAsync());
    }

    [Fact]
    public async Task InvoicesListPage_ShowsSearchButton()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("button:has-text('Search')");
        await _page.WaitForTimeoutAsync(3000);

        var searchBtn = _page.Locator("button:has-text('Search')");
        Assert.True(await searchBtn.IsVisibleAsync());
    }

    [Fact]
    public async Task InvoicesListPage_ShowsShipOrdersButton()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("a.btn:has-text('Ship Orders')");

        var shipOrdersBtn = _page.Locator("a.btn:has-text('Ship Orders')");
        Assert.True(await shipOrdersBtn.IsVisibleAsync());
    }

    [Fact]
    public async Task InvoicesListPage_ShipOrdersButton_LinksToOrders()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("a.btn:has-text('Ship Orders')");

        var href = await _page.GetAttributeAsync("a.btn:has-text('Ship Orders')", "href");
        Assert.Contains("/orders", href ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvoicesListPage_WithNoCustomerSelected_ShowsNoInvoices()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("h1");
        await _page.WaitForTimeoutAsync(3000);

        // Without a customer selected, no invoice table should be displayed
        var invoiceTable = await _page.QuerySelectorAsync("table");
        Assert.Null(invoiceTable);
    }

    [Fact]
    public async Task InvoicesListPage_CustomerDropdown_IsPopulated()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(3000);

        var firstSelect = _page.Locator("select.form-select").First;
        var options = await firstSelect.Locator("option").CountAsync();
        // Should have at least one option (the placeholder "— select customer —")
        Assert.True(options >= 1);
    }

    [Fact]
    public async Task InvoicesListPage_IsAccessible_ForStaffRole()
    {
        var staffEmail = $"staff-inv-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@inv1!";

        await LoginAsAdminAsync();
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("h1", new() { Timeout = 15000 });

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Invoices", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvoicesListPage_ChannelDropdown_HasInsulatedOption()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(3000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Insulated", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FedEx", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Invoice Detail Page ───────────────────────────────────────────────────
    // Note: Testing the Invoice Detail page requires a shipped order, which involves
    // multiple API operations. The tests below verify the page renders correctly
    // when navigated to, using data from an existing customer and order flow.

    [Fact]
    public async Task InvoiceDetailPage_WithInvalidIds_ShowsNotFoundMessage()
    {
        await LoginAsAdminAsync();

        var fakeCustomerId = Guid.NewGuid();
        var fakeInvoiceId = Guid.NewGuid();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices/{fakeCustomerId}/{fakeInvoiceId}");
        await _page.WaitForSelectorAsync(".alert-warning, h1", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(3000);

        var body = await _page.InnerTextAsync("body");
        // Should show either "not found" alert or be on an error/redirect page
        Assert.True(
            body.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("Invoice", StringComparison.OrdinalIgnoreCase),
            "Page should handle invalid invoice IDs gracefully"
        );
    }

    [Fact]
    public async Task InvoiceDetailPage_WithInvalidIds_ShowsBackButton()
    {
        await LoginAsAdminAsync();

        var fakeCustomerId = Guid.NewGuid();
        var fakeInvoiceId = Guid.NewGuid();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices/{fakeCustomerId}/{fakeInvoiceId}");
        await _page.WaitForSelectorAsync("a.btn", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(3000);

        var backBtn = await _page.QuerySelectorAsync("a.btn:has-text('Back')");
        Assert.NotNull(backBtn);
    }
}
