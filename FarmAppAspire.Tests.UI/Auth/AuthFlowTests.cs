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
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Contains("/account/login", _page.Url);
    }

    [Fact]
    public async Task ValidLogin_LandsOnHome_WithUsernameInNav()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        Assert.DoesNotContain("/account/login", _page.Url);
        var navText = await _page.InnerTextAsync("nav");
        Assert.Contains(LoginHelper.AdminEmail, navText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidLogin_ShowsError_StaysOnLogin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, "wrong-password-xyz");

        Assert.Contains("/account/login", _page.Url);
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Invalid email or password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lockout_ShownAfterRepeatedFailedAttempts()
    {
        // Submit wrong password 5 times
        for (var i = 0; i < 5; i++)
        {
            await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
                LoginHelper.AdminEmail, $"wrong-{i}");
        }

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_ClearsSession_RedirectsToLogin()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        Assert.DoesNotContain("/account/login", _page.Url);

        await LoginHelper.LogoutAsync(_page);

        Assert.Contains("/account/login", _page.Url);
    }

    [Fact]
    public async Task FarmAdmin_SeesUsersNavLink()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        var nav = await _page.InnerTextAsync("nav");
        Assert.Contains("Users", nav, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaffUser_DoesNotSeeUsersNavLink()
    {
        var staffEmail = $"staff-nav-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@test1!";

        // Use admin to create staff user
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        // Log in as staff
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);

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
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("not authorized", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User Management", body, StringComparison.OrdinalIgnoreCase);
    }
}
