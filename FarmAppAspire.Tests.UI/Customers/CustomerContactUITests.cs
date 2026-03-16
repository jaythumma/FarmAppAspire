using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.Customers;

/// <summary>
/// Playwright tests for the Customer Contact management UI introduced by
/// add-customer-contact-ui: inline add/edit/delete form, two-step delete
/// confirmation, and the Wholesale customer no-contacts hint.
/// </summary>
[Collection("Playwright")]
public class CustomerContactUITests(AspirePlaywrightFixture fixture) : IAsyncLifetime
{
    private const int CircuitWarmupMs = 4000;
    private const int ShortWaitMs = 2000;

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

    /// <summary>
    /// Creates a new Wholesale customer and navigates to its detail page.
    /// Returns the customer detail URL.
    /// </summary>
    private async Task<string> CreateWholesaleCustomerAndNavigateToDetailAsync(
        string displayName, string companyName = "Contact Test Corp")
    {
        await _page.GotoAsync($"{fixture.BaseUrl}/customers/create");
        await _page.WaitForSelectorAsync("select.form-select");
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);

        await LoginHelper.RetrySelectUntilValueAsync(_page, "select.form-select", "Wholesale");
        await _page.Locator("label:has-text('Display Name') + input.form-control").FillAsync(displayName);

        // Company Name appears after selecting Wholesale
        var companyLabel = _page.Locator("label:has-text('Company Name')").First;
        await companyLabel.WaitForAsync();
        await _page.Locator("label:has-text('Company Name') + input.form-control").First.FillAsync(companyName);

        // Fill required shipping address fields using label-based selectors
        await _page.Locator("label:has-text('Line 1') + input.form-control").First.FillAsync("1 Main St");
        await _page.Locator("label:has-text('City') + input.form-control").FillAsync("Springfield");
        await _page.Locator("select:has(option[value='AL'])").First.SelectOptionAsync("IL");
        await _page.Locator("label:has-text('Postal Code') + input.form-control").FillAsync("62701");
        await _page.Locator("label:has-text('Country') + input.form-control").FillAsync("US");

        await _page.ClickAsync("button[type='submit']:has-text('Save')");
        await _page.WaitForURLAsync(
            url => url.Contains("/customers/") && !url.Contains("/create"),
            new() { Timeout = 30_000 });

        // Wait for the InteractiveServer circuit to render the detail page
        await _page.WaitForTimeoutAsync(CircuitWarmupMs);
        return _page.Url;
    }

    // ── Add Contact button ────────────────────────────────────────────────────

    [Fact]
    public async Task ContactsSection_ShowsAddContactButton_WhenAdmin()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-Add-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        var addBtn = _page.Locator("button:has-text('+ Add Contact')");
        await addBtn.WaitForAsync(new() { Timeout = 15_000 });
        Assert.True(await addBtn.IsVisibleAsync(), "Add Contact button should be visible to FarmAdmin");
    }

    // ── Wholesale hint ────────────────────────────────────────────────────────

    [Fact]
    public async Task WholesaleCustomer_WithNoContacts_ShowsWarningHint()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-Hint-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        var body = await _page.InnerTextAsync("body");
        Assert.Contains(
            "Wholesale customers should have at least one contact",
            body,
            StringComparison.OrdinalIgnoreCase);
    }

    // ── Inline form open / close ──────────────────────────────────────────────

    [Fact]
    public async Task AddContact_ClickButton_OpensInlineForm()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-Form-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        // Click + Add Contact (may need retry until circuit is active)
        await ClickUntilVisibleAsync("button:has-text('+ Add Contact')", "h6:has-text('New Contact')");

        var formTitle = _page.Locator("h6:has-text('New Contact')");
        Assert.True(await formTitle.IsVisibleAsync(), "Inline form with 'New Contact' heading should appear");

        // Form should contain First Name and Last Name labels
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("First Name", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Last Name", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ContactForm_Cancel_HidesForm()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-Cancel-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        // Open the form
        await ClickUntilVisibleAsync("button:has-text('+ Add Contact')", "h6:has-text('New Contact')");

        // Click Cancel
        await _page.ClickAsync("button:has-text('Cancel')");
        await _page.WaitForSelectorAsync(
            "h6:has-text('New Contact')",
            new() { State = WaitForSelectorState.Hidden, Timeout = 10_000 });

        var form = await _page.QuerySelectorAsync("h6:has-text('New Contact')");
        Assert.Null(form);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ContactForm_SubmitWithEmptyFirstName_ShowsValidationError()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-Valid-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        // Open the form
        await ClickUntilVisibleAsync("button:has-text('+ Add Contact')", "h6:has-text('New Contact')");

        // Leave First Name empty; fill Last Name only
        var lastNameInput = _page.Locator("label:has-text('Last Name') + input").First;
        await lastNameInput.FillAsync("Smith");

        // Submit the form
        await _page.Locator(".card-body:has(h6:has-text('New Contact')) button[type='submit']:has-text('Save')").ClickAsync();
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        // Validation message for First Name should appear
        var body = await _page.InnerTextAsync("body");
        Assert.Contains("First name is required", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Full add-contact flow ─────────────────────────────────────────────────

    [Fact]
    public async Task AddContact_WithValidData_AppearsInContactsTable()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-Full-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        // Open the form
        await ClickUntilVisibleAsync("button:has-text('+ Add Contact')", "h6:has-text('New Contact')");

        // Fill in required fields
        var firstNameInput = _page.Locator("label:has-text('First Name') + input").First;
        var lastNameInput  = _page.Locator("label:has-text('Last Name') + input").First;
        await firstNameInput.FillAsync("Alice");
        await lastNameInput.FillAsync("Tester");

        // Submit
        await _page.Locator(".card-body:has(h6:has-text('New Contact')) button[type='submit']:has-text('Save')").ClickAsync();

        // Wait for form to close and table to appear
        await _page.WaitForSelectorAsync(
            "h6:has-text('New Contact')",
            new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Alice", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tester", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Two-step delete confirmation ──────────────────────────────────────────

    [Fact]
    public async Task DeleteContact_FirstClick_ShowsConfirmationButtons()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-Del-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        // First add a contact
        await ClickUntilVisibleAsync("button:has-text('+ Add Contact')", "h6:has-text('New Contact')");
        var firstNameInput = _page.Locator("label:has-text('First Name') + input").First;
        var lastNameInput  = _page.Locator("label:has-text('Last Name') + input").First;
        await firstNameInput.FillAsync("Bob");
        await lastNameInput.FillAsync("Delete");
        await _page.Locator(".card-body:has(h6:has-text('New Contact')) button[type='submit']:has-text('Save')").ClickAsync();
        await _page.WaitForSelectorAsync(
            "h6:has-text('New Contact')",
            new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });

        // Click Delete (first click — should show Confirm? + Cancel)
        var deleteBtn = _page.Locator("button:has-text('Delete')").Last;
        await deleteBtn.WaitForAsync(new() { Timeout = 10_000 });
        await deleteBtn.ClickAsync();
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var confirmBtn = _page.Locator("button:has-text('Confirm?')");
        Assert.True(await confirmBtn.IsVisibleAsync(), "Confirm? button should appear after first Delete click");

        var cancelBtn = _page.Locator("button:has-text('Cancel')").Last;
        Assert.True(await cancelBtn.IsVisibleAsync(), "Cancel button should appear alongside Confirm?");
    }

    [Fact]
    public async Task DeleteContact_CancelConfirmation_RestoresDeleteButton()
    {
        await LoginAsAdminAsync();
        var customerName = $"Contact-DelCan-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateWholesaleCustomerAndNavigateToDetailAsync(customerName);

        // Add a contact first
        await ClickUntilVisibleAsync("button:has-text('+ Add Contact')", "h6:has-text('New Contact')");
        var firstNameInput = _page.Locator("label:has-text('First Name') + input").First;
        var lastNameInput  = _page.Locator("label:has-text('Last Name') + input").First;
        await firstNameInput.FillAsync("Carol");
        await lastNameInput.FillAsync("Cancel");
        await _page.Locator(".card-body:has(h6:has-text('New Contact')) button[type='submit']:has-text('Save')").ClickAsync();
        await _page.WaitForSelectorAsync(
            "h6:has-text('New Contact')",
            new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });

        // Click Delete → shows Confirm?
        var deleteBtn = _page.Locator("button:has-text('Delete')").Last;
        await deleteBtn.WaitForAsync(new() { Timeout = 10_000 });
        await deleteBtn.ClickAsync();
        await _page.WaitForSelectorAsync("button:has-text('Confirm?')", new() { Timeout = 10_000 });

        // Click Cancel → Confirm? disappears, Delete comes back
        await _page.Locator("button:has-text('Cancel')").Last.ClickAsync();
        await _page.WaitForTimeoutAsync(ShortWaitMs);

        var confirm = await _page.QuerySelectorAsync("button:has-text('Confirm?')");
        Assert.Null(confirm);

        var restoreDelete = _page.Locator("button:has-text('Delete')");
        Assert.True(await restoreDelete.IsVisibleAsync(), "Delete button should be restored after cancelling confirmation");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Repeatedly clicks <paramref name="trigger"/> until <paramref name="expected"/>
    /// becomes visible. Handles the Blazor Interactive Server circuit warm-up gap
    /// between page load and the first @onclick being active.
    /// </summary>
    /// <param name="trigger">CSS selector for the element to click.</param>
    /// <param name="expected">CSS selector for the element that must become visible.</param>
    /// <param name="maxAttempts">Maximum number of click attempts before a final wait (default 20).</param>
    /// <param name="intervalMs">Milliseconds to wait for visibility after each click attempt (default 600).</param>
    private async Task ClickUntilVisibleAsync(string trigger, string expected,
        int maxAttempts = 20, int intervalMs = 600)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            await _page.ClickAsync(trigger);
            try
            {
                await _page.WaitForSelectorAsync(expected,
                    new() { State = WaitForSelectorState.Visible, Timeout = intervalMs });
                return;
            }
            catch (TimeoutException)
            {
                await _page.WaitForTimeoutAsync(200);
            }
        }
        // Final attempt — will throw a clear Playwright timeout if element never appears
        await _page.WaitForSelectorAsync(expected,
            new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
    }
}
