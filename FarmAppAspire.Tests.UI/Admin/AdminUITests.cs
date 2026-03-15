using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.Admin;

/// <summary>
/// Playwright tests for the Admin pages: User Management (/admin/users) and
/// Product Catalog (/admin/products).
/// </summary>
[Collection("Playwright")]
public class AdminUITests(AspirePlaywrightFixture fixture) : IAsyncLifetime
{
    private const int CircuitWarmupMs = 4000;
    private const int ShortWaitMs = 3000;
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

    // ── User Management Page (/admin/users) ───────────────────────────────────

    [Fact]
    public async Task UsersPage_Loads_WithHeading()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("h1");

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("User Management", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsersPage_ShowsAddUserButton()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("button:has-text('Add User')");

        var btn = _page.Locator("button:has-text('Add User')");
        Assert.True(await btn.IsVisibleAsync());
    }

    [Fact]
    public async Task UsersPage_ShowsUsersTable()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var table = _page.Locator("table");
        Assert.True(await table.IsVisibleAsync());
    }

    [Fact]
    public async Task UsersPage_TableContainsAdminUser()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var tableText = await _page.InnerTextAsync("table");
        Assert.Contains(LoginHelper.AdminEmail, tableText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsersPage_ClickAddUser_ShowsCreateForm()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("button:has-text('Add User')");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        await _page.ClickAsync("button:has-text('Add User')");
        await _page.WaitForSelectorAsync(".card-body", new() { Timeout = 10000 });

        var cardBody = _page.Locator(".card-body");
        Assert.True(await cardBody.IsVisibleAsync());
    }

    [Fact]
    public async Task UsersPage_CreateForm_HasEmailPasswordRoleFields()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("button:has-text('Add User')");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        await _page.ClickAsync("button:has-text('Add User')");
        await _page.WaitForSelectorAsync(".card-body input", new() { Timeout = 10000 });

        var cardText = await _page.InnerTextAsync(".card-body");
        Assert.Contains("Email", cardText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Password", cardText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Role", cardText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsersPage_CreateForm_CancelButton_HidesForm()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("button:has-text('Add User')");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        await _page.ClickAsync("button:has-text('Add User')");
        await _page.WaitForSelectorAsync(".card-body", new() { Timeout = 10000 });

        await _page.ClickAsync(".card-body button:has-text('Cancel')");
        await _page.WaitForSelectorAsync(".card-body",
            new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });

        var form = await _page.QuerySelectorAsync(".card-body input");
        Assert.Null(form);
    }

    [Fact]
    public async Task UsersPage_CanCreateNewUser()
    {
        var newUserEmail = $"admin-test-{Guid.NewGuid():N}@test.com";
        const string newUserPassword = "Admin@test1!";

        await LoginAsAdminAsync();
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, newUserEmail, newUserPassword, "Staff");

        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var tableText = await _page.InnerTextAsync("table");
        Assert.Contains(newUserEmail, tableText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsersPage_ShowsActiveBadge_ForExistingUsers()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        // Active users should have an "Active" badge in the table
        var activeBadge = await _page.QuerySelectorAsync("table .badge");
        Assert.NotNull(activeBadge);
    }

    [Fact]
    public async Task UsersPage_NonAdmin_ShowsNotAuthorized()
    {
        var staffEmail = $"staff-users-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@test1!";

        await LoginAsAdminAsync();
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/users");
        await _page.WaitForSelectorAsync(".alert-warning, .alert-danger", new() { Timeout = 15000 });

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Not Authorized", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Product Catalog Page (/admin/products) ────────────────────────────────

    [Fact]
    public async Task ProductCatalogPage_Loads_WithHeading()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/products");
        await _page.WaitForSelectorAsync("h1");

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Product Catalog", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductCatalogPage_ShowsInsulatedChannelCard()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/products");
        await _page.WaitForSelectorAsync("h5", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Insulated", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductCatalogPage_ShowsFedExChannelCard()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/products");
        await _page.WaitForSelectorAsync("h5", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("FedEx", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductCatalogPage_ShowsPricingTables()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/products");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var tables = _page.Locator("table");
        var count = await tables.CountAsync();
        Assert.True(count >= 2, "Product catalog should show at least two pricing tables (Insulated + FedEx)");
    }

    [Fact]
    public async Task ProductCatalogPage_InsulatedTable_HasExpectedColumns()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/products");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var firstTableHeader = await _page.Locator("table thead").First.InnerTextAsync();
        Assert.Contains("Size", firstTableHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Weight", firstTableHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductCatalogPage_ShowsDefaultBadge_ForInsulatedBoxes()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/products");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 15000 });
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        // Default badge for the default box size
        var defaultBadge = await _page.QuerySelectorAsync("table .badge:has-text('Default')");
        Assert.NotNull(defaultBadge);
    }

    [Fact]
    public async Task ProductCatalogPage_IsAccessibleByStaffRole()
    {
        var staffEmail = $"staff-catalog-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@catalog1!";

        await LoginAsAdminAsync();
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/products");
        await _page.WaitForSelectorAsync("h1", new() { Timeout = 15000 });

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Product Catalog", heading, StringComparison.OrdinalIgnoreCase);
    }

    // ── Admin Orders Page (/admin/orders) ─────────────────────────────────────

    [Fact]
    public async Task AdminOrdersPage_Loads_WithHeading()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/orders");
        await _page.WaitForSelectorAsync("h1");
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var heading = await _page.InnerTextAsync("h1");
        Assert.False(string.IsNullOrWhiteSpace(heading));
    }

    [Fact]
    public async Task AdminOrdersPage_ShowsGenerateInstancesButton()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/orders");
        await _page.WaitForSelectorAsync("h1");
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Generate Instances", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminOrdersPage_ShowsWeekNavigation()
    {
        await LoginAsAdminAsync();
        await _page.GotoAsync($"{fixture.BaseUrl}/admin/orders");
        await _page.WaitForSelectorAsync("h1");
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var body = await _page.InnerTextAsync("body");
        // Browse Instances section has prev/next week navigation
        Assert.Contains("Previous", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Next", body, StringComparison.OrdinalIgnoreCase);
    }
}
