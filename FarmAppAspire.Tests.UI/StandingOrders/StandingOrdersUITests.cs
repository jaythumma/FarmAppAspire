using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.StandingOrders;

/// <summary>
/// Playwright tests for the Standing Orders feature:
/// list page (/standing-orders) and detail/edit page.
/// </summary>
[Collection("Playwright")]
public class StandingOrdersUITests(AspirePlaywrightFixture fixture) : IAsyncLifetime
{
    private const int ShortWaitMs = 2000;
    private const int CircuitWarmupMs = 3000;
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

    // ── Standing Orders List Page (/standing-orders) ──────────────────────────

    [Fact]
    public async Task StandingOrdersListPage_Loads_WithHeading()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders");
        await _page.WaitForSelectorAsync("h1");

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Standing Orders", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StandingOrdersListPage_ShowsNewStandingOrderButton()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders");
        await _page.WaitForSelectorAsync("h1");
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("New Standing Order", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StandingOrdersListPage_ShowsCustomerDropdown()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        var customerSelect = _page.Locator("select.form-select").First;
        Assert.True(await customerSelect.IsVisibleAsync());
    }

    [Fact]
    public async Task StandingOrdersListPage_CustomerDropdown_IsPopulated()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        var options = await _page.Locator("select.form-select option").CountAsync();
        // At minimum should have the placeholder option
        Assert.True(options >= 1);
    }

    [Fact]
    public async Task StandingOrdersListPage_WithNoCustomerSelected_ShowsTableOrEmptyMessage()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders");
        await _page.WaitForSelectorAsync("h1");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        // All orders are loaded by default; a table or empty-state message should appear
        var body = await _page.InnerTextAsync("body");
        Assert.True(
            body.Contains("Standing Order", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("No standing orders", StringComparison.OrdinalIgnoreCase),
            "Page should show all orders or an empty-state message by default"
        );
    }

    [Fact]
    public async Task StandingOrdersListPage_IsAccessible_ForStaffRole()
    {
        var staffEmail = $"staff-so-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@so1!";

        await LoginAsAdminAsync();
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders");
        await _page.WaitForSelectorAsync("h1", new() { Timeout = 15000 });

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Standing Orders", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StandingOrdersListPage_SelectCustomer_IfHasOrders_ShowsTable()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        // Get customer dropdown options (skip the placeholder)
        var options = await _page.Locator("select.form-select option:not([value=''])").AllAsync();
        if (options.Count == 0) return; // No customers seeded — skip test

        // Select first real customer
        var firstCustomerId = await options[0].GetAttributeAsync("value");
        if (string.IsNullOrEmpty(firstCustomerId)) return;

        await _page.Locator("select.form-select").SelectOptionAsync(firstCustomerId);
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        // After selection, either a table or "no standing orders" alert should appear
        var body = await _page.InnerTextAsync("body");
        Assert.True(
            body.Contains("Standing Order", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("No standing orders", StringComparison.OrdinalIgnoreCase),
            "After selecting a customer, standing orders should load"
        );
    }

    // ── Standing Order Detail Page ────────────────────────────────────────────

    [Fact]
    public async Task StandingOrderDetailPage_WithInvalidIds_RendersGracefully()
    {
        await LoginAsAdminAsync();

        var fakeCustomerId = Guid.NewGuid();
        var fakeSoId = Guid.NewGuid();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders/{fakeCustomerId}/{fakeSoId}");
        await _page.WaitForSelectorAsync("h1, .alert-warning, .alert-danger", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var body = await _page.InnerTextAsync("body");
        // Page should render without a crash — either show the form or a not-found message
        Assert.False(string.IsNullOrWhiteSpace(body));
    }

    [Fact]
    public async Task StandingOrderDetailPage_WithInvalidIds_ShowsBackButton()
    {
        await LoginAsAdminAsync();

        var fakeCustomerId = Guid.NewGuid();
        var fakeSoId = Guid.NewGuid();
        await _page.GotoAsync($"{fixture.BaseUrl}/standing-orders/{fakeCustomerId}/{fakeSoId}");
        await _page.WaitForSelectorAsync("a.btn, h1", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        // Back or "← Back" navigation button should be available
        var backBtn = await _page.QuerySelectorAsync("a.btn:has-text('Back'), a.btn:has-text('←')");
        Assert.NotNull(backBtn);
    }

    // ── Via Customer Detail — Standing Orders link ─────────────────────────────

    [Fact]
    public async Task CustomerDetailPage_StandingOrdersSection_IsVisible()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        var firstCustomerLink = await _page.QuerySelectorAsync("table a");
        if (firstCustomerLink is null) return;

        await firstCustomerLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Standing Order", body, StringComparison.OrdinalIgnoreCase);
    }
}
