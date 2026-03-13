using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FarmAppAspire.Tests.UI.Fixtures;

public sealed class AspirePlaywrightFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = "";
    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.FarmAppAspire_AppHost>(cancellationToken);

        _app = await appHost.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("webfrontend", cancellationToken);

        BaseUrl = _app.GetEndpoint("webfrontend").AbsoluteUri.TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        // The IdentitySeeder runs as a background IHostedService AFTER the /health
        // check passes. Poll with Playwright until the admin login succeeds so that
        // no test starts before the seed data is committed to the identity DB.
        await WaitForIdentitySeedAsync(cancellationToken);
    }

    /// <summary>
    /// Retries the admin login (using Playwright) until it succeeds or 120 s elapses.
    /// A successful login means the browser is redirected away from the login page,
    /// confirming the IdentitySeeder has created the admin user.
    /// </summary>
    private async Task WaitForIdentitySeedAsync(CancellationToken ct)
    {
        var ctx  = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(10_000); // short timeout per attempt

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(120);
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                try
                {
                    await page.GotoAsync($"{BaseUrl}/account/login");
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    await page.FillAsync("input[name='Email']",    LoginHelper.AdminEmail);
                    await page.FillAsync("input[name='Password']", LoginHelper.AdminPassword);
                    await page.ClickAsync("button[type='submit']");
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    // If we left the login page the credentials worked → seed is done
                    if (!page.Url.Contains("/account/login", StringComparison.OrdinalIgnoreCase))
                        return;
                }
                catch
                {
                    // Navigation timeout or element not found — app still starting
                }

                await Task.Delay(2_000, ct);
            }
        }
        finally
        {
            await page.CloseAsync();
            await ctx.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null) await Browser.DisposeAsync();
        _playwright?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }
}



