using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.Auth;

[Collection("Playwright")]
public class AuthFlowTests(AspirePlaywrightFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task UnauthenticatedUser_RedirectedToLogin()
    {
        await _page.GotoAsync($"{fixture.BaseUrl}/");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Assert.Contains("/account/login", _page.Url);
    }

    [Fact]
    public async Task ValidLogin_LandsOnHome_WithUsernameInNav()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        // AuthorizeView renders the username server-side; wait for it to appear in nav
        await _page.WaitForSelectorAsync($"nav:has-text('{LoginHelper.AdminEmail}')");

        Assert.DoesNotContain("/account/login", _page.Url);
        var navText = await _page.InnerTextAsync("nav");
        Assert.Contains(LoginHelper.AdminEmail, navText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidLogin_ShowsError_StaysOnLogin()
    {
        // Submit directly without the helper so we can control the wait precisely
        await _page.GotoAsync($"{fixture.BaseUrl}/account/login");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.FillAsync("input[name='Email']", LoginHelper.AdminEmail);
        await _page.FillAsync("input[name='Password']", "wrong-password-xyz");
        await _page.ClickAsync("button[type='submit']");
        // Login page has Layout = null (no Blazor scripts) — Load state fires after the POST response
        await _page.WaitForLoadStateAsync(LoadState.Load);

        Assert.Contains("/account/login", _page.Url, StringComparison.OrdinalIgnoreCase);
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Invalid email or password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lockout_ShownAfterRepeatedFailedAttempts()
    {
        // Use a dedicated single-use account so FarmAdmin is never locked across tests
        var lockoutEmail = $"lockout-{Guid.NewGuid():N}@test.com";
        const string lockoutPassword = "Lockout@test1!";

        await using var adminCtx = await fixture.Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var adminPage = await adminCtx.NewPageAsync();
        await LoginHelper.LoginAsync(adminPage, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await LoginHelper.CreateUserAsync(adminPage, fixture.BaseUrl, lockoutEmail, lockoutPassword, "Staff");

        // Submit wrong password MaxFailedAccessAttempts (5) times
        for (var i = 0; i < 5; i++)
        {
            await _page.GotoAsync($"{fixture.BaseUrl}/account/login");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await _page.FillAsync("input[name='Email']", lockoutEmail);
            await _page.FillAsync("input[name='Password']", $"wrong-{i}");
            await _page.ClickAsync("button[type='submit']");
            await _page.WaitForLoadStateAsync(LoadState.Load);
        }

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_ClearsSession_RedirectsToLogin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync($"nav:has-text('{LoginHelper.AdminEmail}')");

        await LoginHelper.LogoutAsync(_page);
        await _page.WaitForURLAsync("**/account/login");

        Assert.Contains("/account/login", _page.Url);
    }

    [Fact]
    public async Task FarmAdmin_SeesUsersNavLink()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.WaitForSelectorAsync("nav a[href='admin/users']");

        var nav = await _page.InnerTextAsync("nav");
        Assert.Contains("Users", nav, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaffUser_DoesNotSeeUsersNavLink()
    {
        var staffEmail = $"staff-nav-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@test1!";

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.WaitForSelectorAsync($"nav:has-text('{staffEmail}')");

        var nav = await _page.InnerTextAsync("nav");
        Assert.DoesNotContain("Users", nav, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaffUser_AdminUsersPage_ShowsNotAuthorized()
    {
        var staffEmail = $"staff-authz-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@test1!";

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        // Blazor SSR with AuthorizeRouteView renders NotAuthorizedView synchronously;
        // use a generous timeout since it still needs a circuit render cycle.
        await _page.WaitForSelectorAsync(".alert-warning, .alert-danger", new() { Timeout = 15000 });

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Not Authorized", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User Management", body, StringComparison.OrdinalIgnoreCase);
    }
}
