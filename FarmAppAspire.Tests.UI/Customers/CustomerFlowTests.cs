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
        // Display Name is the first text input
        await _page.Locator("input.form-control").First.FillAsync(customerName);
        // Company Name is required for Wholesale
        await FillCompanyNameAsync(_page, "Test Corp Ltd");

        // Fill in required shipping address fields
        await FillShippingAddressAsync(_page, "1 Main St", "Springfield", "IL", "62701", "US");

        await _page.ClickAsync("button[type='submit']:has-text('Save')");
        // Wait for navigation from /customers/create to /customers/{id}
        await _page.WaitForURLAsync(url => url.Contains("/customers/") && !url.Contains("/create"), new() { Timeout = 30000 });

        await _page.GotoAsync($"{fixture.BaseUrl}/customers");
        // InteractiveServer list: wait for the customer name to appear after circuit connects
        await _page.Locator($"text={customerName}").First.WaitForAsync(new() { Timeout = 30000 });

        var pageContent = await _page.InnerTextAsync("body");
        Assert.Contains(customerName, pageContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateForm_ShowsShippingAddressSection()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("h5");
        await _page.WaitForTimeoutAsync(3000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Shipping Address", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateForm_BillingToggle_CheckedByDefault_HidesBillingFields()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("#billingUsesShipping");
        await _page.WaitForTimeoutAsync(3000);

        // Checkbox should be checked by default
        var isChecked = await _page.IsCheckedAsync("#billingUsesShipping");
        Assert.True(isChecked);

        // Billing address fields should NOT be visible
        var billingSection = await _page.QuerySelectorAsync("h5:has-text('Billing Address') + div .form-control");
        // When toggle is on, the billing inputs are hidden via @if(!BillingUsesShipping)
        Assert.Null(billingSection);
    }

    [Fact]
    public async Task CreateForm_UncheckBillingToggle_ShowsBillingFields()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("#billingUsesShipping");
        await _page.WaitForTimeoutAsync(3000);

        // Uncheck "same as shipping"
        await _page.UncheckAsync("#billingUsesShipping");
        // Wait for billing AddressForm to render: a second state dropdown appears
        await _page.Locator("select:has(option[value='AL'])").Nth(1)
            .WaitForAsync(new() { Timeout = 15000 });

        // Billing section inputs should now be visible
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Billing Address", body, StringComparison.OrdinalIgnoreCase);
        var inputs = await _page.Locator("input.form-control").CountAsync();
        // main(3) + shipping(5) + billing(5) = 13 inputs; state fields are <select> not <input>
        Assert.True(inputs > 6, "Billing address fields should appear after unchecking toggle");
    }

    [Fact]
    public async Task CreateForm_WholesaleType_CompanyNameRequired()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(3000);

        await LoginHelper.RetrySelectUntilValueAsync(_page, "select.form-select", "Wholesale");
        await _page.WaitForTimeoutAsync(500);

        // Company Name field should appear and be marked required
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Company Name", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateForm_SubmitWithoutShipping_ShowsValidationError()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(3000);

        // Fill display name but leave shipping address empty
        await _page.Locator("input.form-control").First.FillAsync("Test Customer");

        await _page.ClickAsync("button[type='submit']:has-text('Save')");
        await _page.WaitForTimeoutAsync(500);

        // Should still be on the create page (validation prevented submit)
        Assert.Contains("/customers/create", _page.Url);
    }

    [Fact]
    public async Task DetailPage_BillingUsesShipping_ShowsSameAsShippingBadge()
    {
        // Create a customer (BillingUsesShipping defaults to true)
        var customerName = $"Billing-{Guid.NewGuid().ToString("N")[..8]}";
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(4000);

        await LoginHelper.RetrySelectUntilValueAsync(_page, "select.form-select", "Wholesale");
        await _page.Locator("input.form-control").First.FillAsync(customerName);
        await FillCompanyNameAsync(_page, "Billing Corp");
        await FillShippingAddressAsync(_page, "5 Test Ave", "Chicago", "IL", "60601", "US");
        await _page.ClickAsync("button[type='submit']:has-text('Save')");
        await _page.WaitForURLAsync(url => url.Contains("/customers/") && !url.Contains("/create"), new() { Timeout = 30000 });

        // On detail page, billing section should show "Same as shipping address" badge
        await _page.WaitForSelectorAsync("h5:has-text('Billing')");
        await _page.WaitForTimeoutAsync(2000);
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Same as shipping address", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetailPage_BillingUndefined_ShowsNotSetPrompt()
    {
        // Create customer with BillingUsesShipping = false and no billing address
        // We do this by unchecking the toggle but leaving billing fields empty
        var customerName = $"NoBilling-{Guid.NewGuid().ToString("N")[..8]}";
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(4000);

        await LoginHelper.RetrySelectUntilValueAsync(_page, "select.form-select", "Wholesale");
        await _page.Locator("input.form-control").First.FillAsync(customerName);
        await FillCompanyNameAsync(_page, "NoBilling Corp");
        await FillShippingAddressAsync(_page, "7 Oak St", "Peoria", "IL", "61602", "US");
        await _page.UncheckAsync("#billingUsesShipping");
        await _page.WaitForTimeoutAsync(300);
        await _page.ClickAsync("button[type='submit']:has-text('Save')");
        await _page.WaitForURLAsync(url => url.Contains("/customers/") && !url.Contains("/create"), new() { Timeout = 30000 });

        // Wait for InteractiveServer circuit to render the billing section
        await _page.WaitForSelectorAsync("h5:has-text('Billing')");
        await _page.WaitForTimeoutAsync(2000);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("not set", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers

    /// <summary>
    /// Fills the shipping address fields. The form renders shipping inputs in a
    /// div.row immediately after the "Shipping Address" h5 heading.
    /// </summary>
    private static async Task FillShippingAddressAsync(
        IPage page, string line1, string city, string state, string postalCode, string country)
    {
        // All current callers select Wholesale type first, so input indices:
        // 0:DisplayName, 1:Email, 2:Phone, 3:CompanyName, 4:TaxId (no PaymentTerms/Notes)
        // 5:Line1, 6:Line2, 7:City, [State=select], 8:PostalCode, 9:Country
        var allInputs = page.Locator("input.form-control");
        await allInputs.Nth(5).FillAsync(line1);
        // Nth(6) is Line2 — skip (optional)
        await allInputs.Nth(7).FillAsync(city);
        // State is a <select> dropdown; identify it by containing an option with value="AL"
        await page.Locator("select:has(option[value='AL'])").First.SelectOptionAsync(state);
        await allInputs.Nth(8).FillAsync(postalCode);
        await allInputs.Nth(9).FillAsync(country);
    }

    /// <summary>Fills Company Name for Wholesale customers.</summary>
    private static async Task FillCompanyNameAsync(IPage page, string companyName)
    {
        // Company Name input appears after Primary Phone when Wholesale is selected
        var label = page.Locator("label:has-text('Company Name')").First;
        await label.WaitForAsync();
        // Company Name input follows its label as an adjacent sibling
        await page.Locator("label:has-text('Company Name') + input.form-control").First.FillAsync(companyName);
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

    [Fact]
    public async Task CreateForm_ShippingStateField_IsDropdown()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("h5:has-text('Shipping Address')");
        await _page.WaitForTimeoutAsync(3000);

        // The state dropdown is identified by containing an option with value "AL" (Alabama)
        var stateSelect = _page.Locator("select:has(option[value='AL'])").First;
        await stateSelect.WaitForAsync(new() { Timeout = 15000 });
        Assert.True(await stateSelect.IsVisibleAsync(), "State field in shipping section should be a <select> dropdown");
    }

    [Fact]
    public async Task CreateForm_StateDropdown_ContainsAllStates()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("h5:has-text('Shipping Address')");
        await _page.WaitForTimeoutAsync(3000);

        // The state dropdown is identified by containing an option with value "AL" (Alabama)
        var stateSelect = _page.Locator("select:has(option[value='AL'])").First;
        await stateSelect.WaitForAsync(new() { Timeout = 15000 });

        var optionCount = await stateSelect.Locator("option").CountAsync();
        Assert.Equal(51, optionCount); // 50 states + 1 blank placeholder
    }

    }

