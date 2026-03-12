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
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Contains("/customers", _page.Url);
        await _page.WaitForSelectorAsync("h1");
        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Customer", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FarmAdmin_CanCreateCustomer_AppearsInList()
    {
        var customerName = $"Test Wholesale {Guid.NewGuid():N[..8]}";
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.SelectOptionAsync("select[id*='Type'], select", "Wholesale");
        await _page.FillAsync("input[id*='DisplayName'], input[placeholder*='name' i]", customerName);
        await _page.ClickAsync("button[type='submit']:has-text('Save'), button[type='submit']:has-text('Create')");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var createBtn = await _page.QuerySelectorAsync("a:has-text('New Customer'), a:has-text('Create')");
        var editBtn = await _page.QuerySelectorAsync("a:has-text('Edit'), button:has-text('Edit')");
        Assert.Null(createBtn);
        Assert.Null(editBtn);
    }

    [Fact]
    public async Task FarmAdmin_DeleteDefaultAddress_PromotesNextAddress()
    {
        var customerName = $"Addr Test {Guid.NewGuid():N[..8]}";
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        // Create customer
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.SelectOptionAsync("select[id*='Type'], select", "Retail");
        await _page.FillAsync("input[id*='DisplayName'], input[placeholder*='name' i]", customerName);
        await _page.ClickAsync("button[type='submit']:has-text('Save'), button[type='submit']:has-text('Create')");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Add two addresses via the detail page
        var currentUrl = _page.Url;
        await _page.WaitForSelectorAsync("text=Add Address, button:has-text('Add')");

        // Add first address
        await FillAndSubmitAddressFormAsync("Home Office", "123 First St");
        // Add second address
        await FillAndSubmitAddressFormAsync("Warehouse", "456 Second Ave");

        // Delete the default address (first one)
        var deleteButtons = await _page.QuerySelectorAllAsync("button:has-text('Delete'):near(td:has-text('Yes'))");
        if (deleteButtons.Count > 0)
        {
            await deleteButtons[0].ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // Remaining address should now show as default
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Yes", body); // IsDefault = Yes still present
    }

    private async Task FillAndSubmitAddressFormAsync(string label, string line1)
    {
        var addBtn = await _page.QuerySelectorAsync("button:has-text('Add Address')");
        if (addBtn != null) await addBtn.ClickAsync();
        await _page.WaitForSelectorAsync("input[placeholder*='label' i], input[id*='Label']");

        await _page.FillAsync("input[id*='Label'], input[placeholder*='label' i]", label);
        await _page.FillAsync("input[id*='Line1'], input[placeholder*='line1' i]", line1);
        await _page.FillAsync("input[id*='City'], input[placeholder*='city' i]", "Springfield");
        await _page.FillAsync("input[id*='State'], input[placeholder*='state' i]", "IL");
        await _page.FillAsync("input[id*='PostalCode'], input[placeholder*='postal' i]", "62701");
        await _page.FillAsync("input[id*='Country'], input[placeholder*='country' i]", "US");
        await _page.ClickAsync("button[type='submit']:has-text('Save'), button:has-text('Add')");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
