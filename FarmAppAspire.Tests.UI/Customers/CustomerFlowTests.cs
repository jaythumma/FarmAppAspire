using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.Customers;

[Collection("Playwright")]
public class CustomerFlowTests(AspirePlaywrightFixture fixture) : IAsyncLifetime
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
    public async Task FarmAdmin_CanSeeCustomersList()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForSelectorAsync("h1");

        Assert.Contains("/customers", _page.Url);
        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Customer", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FarmAdmin_CanCreateCustomer_AppearsInList()
    {
        var customerName = $"Wholesale {Guid.NewGuid().ToString("N")[..8]}";
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("select.form-select");
        // CreateEdit.razor is InteractiveServer; wait for circuit before InputSelect/InputText
        await _page.WaitForTimeoutAsync(4000);

        await LoginHelper.RetrySelectUntilValueAsync(_page, "select.form-select", "Wholesale");
        // InputText renders without explicit type in .NET 10 Blazor; target by position
        await _page.Locator("input.form-control").First.FillAsync(customerName);
        await _page.ClickAsync("button[type='submit']:has-text('Save')");
        await _page.WaitForURLAsync("**/customers/**");

        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForSelectorAsync("h1");

        var pageContent = await _page.InnerTextAsync("body");
        Assert.Contains(customerName, pageContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Staff_CustomerListVisible_NoCreateButton()
    {
        var staffEmail = $"staff-cust-{Guid.NewGuid():N}@test.com";
        const string staffPassword = "Staff@test1!";

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, staffEmail, staffPassword, "Staff");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, staffEmail, staffPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForSelectorAsync("h1");

        // List is visible
        Assert.Contains("/customers", _page.Url);

        // No create button
        var createBtn = await _page.QuerySelectorAsync("a:has-text('New Customer'), a:has-text('Create')");
        Assert.Null(createBtn);
    }

    [Fact]
    public async Task ReadOnly_NoCreateOrEditControls()
    {
        var roEmail = $"readonly-{Guid.NewGuid():N}@test.com";
        const string roPassword = "ReadOnly@test1!";

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await LoginHelper.CreateUserAsync(_page, fixture.BaseUrl, roEmail, roPassword, "ReadOnly");
        await LoginHelper.LogoutAsync(_page);

        await LoginHelper.LoginAsync(_page, fixture.BaseUrl, roEmail, roPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForSelectorAsync("h1");

        var createBtn = await _page.QuerySelectorAsync("a:has-text('New Customer'), a:has-text('Create')");
        var editBtn = await _page.QuerySelectorAsync("a:has-text('Edit'), button:has-text('Edit')");
        Assert.Null(createBtn);
        Assert.Null(editBtn);
    }

    // NOTE: Address management (add/delete/promote) is API-level behaviour verified in
    // FarmAppAspire.Tests (CustomerEndpointTests). The Detail.razor page is read-only;
    // no "Add Address" UI exists, so this scenario is not testable via Playwright.
    // [Fact]
    // public async Task FarmAdmin_DeleteDefaultAddress_PromotesNextAddress() { ... }

    }

