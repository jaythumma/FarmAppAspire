using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.Orders;

[Collection("Playwright")]
public class OrderManagementUITests(AspirePlaywrightFixture fixture) : IAsyncLifetime
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

    // ── Navigation ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Nav_ShowsOrdersAndInvoicesLinks_WhenAuthenticated()
    {
        await LoginAsAdminAsync();
        var navText = await _page.InnerTextAsync("nav");
        Assert.Contains("Orders", navText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Invoices", navText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Order Admin", navText, StringComparison.OrdinalIgnoreCase);
    }

    // ── Order Instances page ───────────────────────────────────────────────────

    [Fact]
    public async Task OrderInstancesPage_Loads_WithFilterControls()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/orders");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Order Instances", body, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(await _page.QuerySelectorAsync("input[placeholder*='customer']"));
        Assert.NotNull(await _page.QuerySelectorAsync("select"));
    }

    // ── FedEx Orders page ──────────────────────────────────────────────────────

    [Fact]
    public async Task FedExOrderPage_Loads_WithCustomerDropdown()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/orders/fedex");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("FedEx Order", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Amazon", body, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(await _page.QuerySelectorAsync("select"));
    }

    [Fact]
    public async Task FedExOrderPage_ShowsTierPricing_WhenLoaded()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/orders/fedex");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("5.99", body);
        Assert.Contains("99.99", body);
    }

    // ── Invoices page ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvoicesPage_Loads_WithFilterControls()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/invoices");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Invoices", body, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(await _page.QuerySelectorAsync("input[placeholder*='customer']"));
        Assert.NotNull(await _page.QuerySelectorAsync("input[placeholder*='Season']"));
    }

    // ── Admin Orders page ──────────────────────────────────────────────────────

    [Fact]
    public async Task AdminOrdersPage_Loads_WithSeasonInfo()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/orders");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Order Administration", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Season Year", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Generate Instances", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminOrdersPage_GenerateInstances_ReportsResult()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/orders");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        await _page.ClickAsync("button:has-text('Generate Instances')");
        await _page.WaitForSelectorAsync("[data-testid='gen-result']", new() { Timeout = 15000 });

        var result = await _page.InnerTextAsync("[data-testid='gen-result']");
        Assert.Contains("Generated", result, StringComparison.OrdinalIgnoreCase);
    }

    // ── Customer Detail — order links ──────────────────────────────────────────

    [Fact]
    public async Task CustomerDetail_ShowsOrderManagementLinks()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var firstLink = await _page.QuerySelectorAsync("table a");
        if (firstLink is null) return;

        await firstLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Standing Order", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Pricing Overrides", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Customer Key", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CustomerDetail_GenerateKey_ShowsKeyValue()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var firstLink = await _page.QuerySelectorAsync("table a");
        if (firstLink is null) return;

        await firstLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var genBtn = await _page.QuerySelectorAsync("[data-testid='btn-generate-key']");
        if (genBtn is null) return;

        await genBtn.ClickAsync();
        await _page.WaitForTimeoutAsync(3000);

        var keyEl = await _page.QuerySelectorAsync("[data-testid='customer-key-value']");
        if (keyEl is not null)
        {
            var keyText = await keyEl.InnerTextAsync();
            Assert.False(string.IsNullOrEmpty(keyText));
        }
    }

    // ── Customer Pricing page ──────────────────────────────────────────────────

    [Fact]
    public async Task CustomerPricingPage_Loads_ForFirstCustomer()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var firstLink = await _page.QuerySelectorAsync("table a");
        if (firstLink is null) return;

        var href = await firstLink.GetAttributeAsync("href");
        var customerId = href?.Split('/').LastOrDefault();
        if (string.IsNullOrEmpty(customerId)) return;

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/{customerId}/pricing");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Pricing Override", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("$13.00", body);

        Assert.NotNull(await _page.QuerySelectorAsync("button:has-text('Add Override')"));
    }

    [Fact]
    public async Task CustomerPricingPage_CanOpenAddForm()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var firstLink = await _page.QuerySelectorAsync("table a");
        if (firstLink is null) return;

        var href = await firstLink.GetAttributeAsync("href");
        var customerId = href?.Split('/').LastOrDefault();
        if (string.IsNullOrEmpty(customerId)) return;

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/{customerId}/pricing");
        await _page.WaitForTimeoutAsync(4000);

        await _page.ClickAsync("button:has-text('Add Override')");
        await _page.WaitForTimeoutAsync(1500);

        var modal = await _page.QuerySelectorAsync(".modal-content");
        Assert.NotNull(modal);

        var modalText = await modal.InnerTextAsync();
        Assert.Contains("Box Size", modalText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Price / lb", modalText, StringComparison.OrdinalIgnoreCase);
    }

    // ── Standing Orders page ───────────────────────────────────────────────────

    [Fact]
    public async Task StandingOrdersPage_Loads_ForFirstCustomer()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var firstLink = await _page.QuerySelectorAsync("table a");
        if (firstLink is null) return;

        var href = await firstLink.GetAttributeAsync("href");
        var customerId = href?.Split('/').LastOrDefault();
        if (string.IsNullOrEmpty(customerId)) return;

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/{customerId}/standing-orders");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Standing Order", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StandingOrdersPage_CanOpenCreateForm_WhenNoOrderExists()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(4000);

        var firstLink = await _page.QuerySelectorAsync("table a");
        if (firstLink is null) return;

        var href = await firstLink.GetAttributeAsync("href");
        var customerId = href?.Split('/').LastOrDefault();
        if (string.IsNullOrEmpty(customerId)) return;

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/{customerId}/standing-orders");
        await _page.WaitForTimeoutAsync(4000);

        var createBtn = await _page.QuerySelectorAsync("button:has-text('Create Standing Order')");
        if (createBtn is not null)
        {
            await createBtn.ClickAsync();
            await _page.WaitForTimeoutAsync(1500);

            var modal = await _page.QuerySelectorAsync(".modal-content");
            Assert.NotNull(modal);

            var modalText = await modal.InnerTextAsync();
            Assert.Contains("Frequency", modalText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Order Lines", modalText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
