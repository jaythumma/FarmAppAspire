using FarmAppAspire.Tests.UI.Fixtures;

namespace FarmAppAspire.Tests.UI.General;

/// <summary>
/// Playwright tests for the general/public pages: Home, Counter, and Weather.
/// </summary>
[Collection("Playwright")]
public class GeneralPagesTests(AspirePlaywrightFixture fixture) : IAsyncLifetime
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

    // ── Home Page ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task HomePage_Loads_WithWelcomeHeading()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/");
        await _page.WaitForSelectorAsync("h1");

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Hello", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_Url_IsRoot()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/");
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // After successful login and navigation to root, URL should be the root
        Assert.DoesNotContain("/account/login", _page.Url);
    }

    [Fact]
    public async Task HomePage_ShowsWelcomeMessage()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/");
        await _page.WaitForSelectorAsync("h1");

        var body = await _page.InnerTextAsync("body");
        Assert.Contains("Welcome", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Counter Page ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CounterPage_Loads_WithHeading()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/counter");
        await _page.WaitForSelectorAsync("h1");

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Counter", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CounterPage_ShowsInitialCount_AsZero()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/counter");
        await _page.WaitForSelectorAsync("[role='status']");

        var status = await _page.InnerTextAsync("[role='status']");
        Assert.Contains("0", status);
    }

    [Fact]
    public async Task CounterPage_ClickButton_IncrementsCount()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/counter");
        await _page.WaitForSelectorAsync("button.btn-primary:has-text('Click me')");
        // Wait for InteractiveServer circuit to connect before clicking
        await _page.WaitForTimeoutAsync(3000);

        await _page.ClickAsync("button.btn-primary:has-text('Click me')");
        await _page.WaitForTimeoutAsync(500);

        var status = await _page.InnerTextAsync("[role='status']");
        Assert.Contains("1", status);
    }

    [Fact]
    public async Task CounterPage_MultipleClicks_AccumulatesCount()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/counter");
        await _page.WaitForSelectorAsync("button.btn-primary:has-text('Click me')");
        await _page.WaitForTimeoutAsync(3000);

        for (var clickCount = 0; clickCount < 3; clickCount++)
        {
            await _page.ClickAsync("button.btn-primary:has-text('Click me')");
            await _page.WaitForTimeoutAsync(300);
        }

        var status = await _page.InnerTextAsync("[role='status']");
        Assert.Contains("3", status);
    }

    [Fact]
    public async Task CounterPage_HasClickMeButton()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/counter");
        await _page.WaitForSelectorAsync("button.btn-primary");

        var button = _page.Locator("button.btn-primary:has-text('Click me')");
        Assert.True(await button.IsVisibleAsync());
    }

    // ── Weather Page ──────────────────────────────────────────────────────────

    [Fact]
    public async Task WeatherPage_Loads_WithHeading()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/weather");
        await _page.WaitForSelectorAsync("h1");

        var heading = await _page.InnerTextAsync("h1");
        Assert.Contains("Weather", heading, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WeatherPage_ShowsDataTable_AfterLoad()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/weather");
        // Weather data loads from the backend API — wait for table to appear
        await _page.WaitForSelectorAsync("table", new() { Timeout = 30000 });

        var table = _page.Locator("table");
        Assert.True(await table.IsVisibleAsync());
    }

    [Fact]
    public async Task WeatherPage_TableHasExpectedColumns()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/weather");
        await _page.WaitForSelectorAsync("table thead", new() { Timeout = 30000 });

        var headerText = await _page.InnerTextAsync("table thead");
        Assert.Contains("Date", headerText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Temp", headerText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summary", headerText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WeatherPage_TableHasDataRows()
    {
        await LoginHelper.LoginAsync(_page, fixture.BaseUrl,
            LoginHelper.AdminEmail, LoginHelper.AdminPassword);

        await _page.GotoAsync($"{fixture.BaseUrl}/weather");
        await _page.WaitForSelectorAsync("table tbody tr", new() { Timeout = 30000 });

        var rowCount = await _page.Locator("table tbody tr").CountAsync();
        Assert.True(rowCount > 0, "Weather table should have at least one data row");
    }
}
