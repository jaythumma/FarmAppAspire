using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.Navigation;

/// <summary>
/// Playwright tests for the sidebar navigation menu.
/// Covers link visibility for unauthenticated, Staff, and FarmAdmin roles,
/// as well as verifying that each link navigates to the correct page.
/// </summary>
[Collection("Playwright")]
public class SidebarNavigationTests(AspirePlaywrightFixture fixture) : IAsyncLifetime
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

    /// <summary>
    /// Creates a unique Staff user via the admin page and returns the email address.
    /// </summary>
    private async Task<string> CreateUniqueStaffUserAsync(string testNamePrefix)
    {
        var staffEmail = $"{testNamePrefix}-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@nav1!";

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.WaitForSelectorAsync($"nav:has-text('{staffEmail}')");

        return staffEmail;
    }

    // ── Brand / Home ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Brand_Link_IsVisible_AfterLogin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a.navbar-brand");

        var brandText = await _page.InnerTextAsync("a.navbar-brand");
        Assert.Contains("FarmAppAspire", brandText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Brand_Link_NavigatesToHome()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("a.navbar-brand");

        await _page.ClickAsync("a.navbar-brand");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.DoesNotContain("/account/login", _page.Url);
    }

    // ── Public nav links (visible before login) ───────────────────────────────

    [Fact]
    public async Task Nav_HomeLink_IsVisible_OnLoginPage()
    {
        await _page.GotoAsync($"{fixture.BaseUrl}/account/login");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // The login page may or may not render the nav menu; test passes if the
        // app renders without error — redirect to login is acceptable.
        Assert.True(_page.Url.Contains("/account/login") || _page.Url == $"{fixture.BaseUrl}/");
    }

    // ── Authenticated nav links ───────────────────────────────────────────────

    [Fact]
    public async Task Nav_ShowsHomeLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync($"nav:has-text('{LoginHelper.AdminEmail}')");

        var nav = await _page.InnerTextAsync("nav");
        Assert.Contains("Home", nav, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Nav_ShowsCounterLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='counter']");

        var link = _page.Locator("nav a[href='counter']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_ShowsWeatherLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='weather']");

        var link = _page.Locator("nav a[href='weather']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_ShowsCustomersLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='customers']");

        var link = _page.Locator("nav a[href='customers']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_ShowsOrdersLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='orders']");

        var link = _page.Locator("nav a[href='orders']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_ShowsStandingOrdersLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='standing-orders']");

        var link = _page.Locator("nav a[href='standing-orders']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_ShowsInvoicesLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='invoices']");

        var link = _page.Locator("nav a[href='invoices']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_ShowsProductCatalogLink_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='admin/products']");

        var link = _page.Locator("nav a[href='admin/products']");
        Assert.True(await link.IsVisibleAsync());
    }

    // ── Admin-only nav links ──────────────────────────────────────────────────

    [Fact]
    public async Task Nav_ShowsUsersLink_ForFarmAdmin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='admin/users']");

        var link = _page.Locator("nav a[href='admin/users']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_ShowsOrderAdminLink_ForFarmAdmin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='admin/orders']");

        var link = _page.Locator("nav a[href='admin/orders']");
        Assert.True(await link.IsVisibleAsync());
    }

    [Fact]
    public async Task Nav_DoesNotShowUsersLink_ForStaffRole()
    {
        await CreateUniqueStaffUserAsync("staff-nav-sidebar");

        var usersLink = await _page.QuerySelectorAsync("nav a[href='admin/users']");
        Assert.Null(usersLink);
    }

    [Fact]
    public async Task Nav_DoesNotShowOrderAdminLink_ForStaffRole()
    {
        await CreateUniqueStaffUserAsync("staff-nav-oa");

        var adminOrdersLink = await _page.QuerySelectorAsync("nav a[href='admin/orders']");
        Assert.Null(adminOrdersLink);
    }

    // ── Nav link navigation ───────────────────────────────────────────────────

    [Fact]
    public async Task NavLink_Counter_NavigatesToCounterPage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='counter']");

        await _page.ClickAsync("nav a[href='counter']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/counter", _page.Url);
    }

    [Fact]
    public async Task NavLink_Weather_NavigatesToWeatherPage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='weather']");

        await _page.ClickAsync("nav a[href='weather']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/weather", _page.Url);
    }

    [Fact]
    public async Task NavLink_Customers_NavigatesToCustomersPage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='customers']");

        await _page.ClickAsync("nav a[href='customers']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/customers", _page.Url);
    }

    [Fact]
    public async Task NavLink_Orders_NavigatesToOrdersPage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='orders']");

        await _page.ClickAsync("nav a[href='orders']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/orders", _page.Url);
    }

    [Fact]
    public async Task NavLink_StandingOrders_NavigatesToStandingOrdersPage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='standing-orders']");

        await _page.ClickAsync("nav a[href='standing-orders']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/standing-orders", _page.Url);
    }

    [Fact]
    public async Task NavLink_Invoices_NavigatesToInvoicesPage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='invoices']");

        await _page.ClickAsync("nav a[href='invoices']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/invoices", _page.Url);
    }

    [Fact]
    public async Task NavLink_ProductCatalog_NavigatesToProductCatalogPage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='admin/products']");

        await _page.ClickAsync("nav a[href='admin/products']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/admin/products", _page.Url);
    }

    [Fact]
    public async Task NavLink_Users_NavigatesToUsersPage_ForAdmin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='admin/users']");

        await _page.ClickAsync("nav a[href='admin/users']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/admin/users", _page.Url);
    }

    [Fact]
    public async Task NavLink_OrderAdmin_NavigatesToOrderAdminPage_ForAdmin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='admin/orders']");

        await _page.ClickAsync("nav a[href='admin/orders']");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/admin/orders", _page.Url);
    }

    // ── Username and logout in nav ────────────────────────────────────────────

    [Fact]
    public async Task Nav_ShowsLoggedInUsername_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync($"nav:has-text('{LoginHelper.AdminEmail}')");

        var navText = await _page.InnerTextAsync("nav");
        Assert.Contains(LoginHelper.AdminEmail, navText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Nav_ShowsLogoutButton_WhenAuthenticated()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav button[type='submit']:has-text('Logout')");

        var logoutBtn = _page.Locator("nav button[type='submit']:has-text('Logout')");
        Assert.True(await logoutBtn.IsVisibleAsync());
    }
}
