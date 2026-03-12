namespace FarmAppAspire.Tests.UI.Fixtures;

public static class LoginHelper
{
    public const string AdminEmail = "admin@farm.local";
    public const string AdminPassword = "Farm@dev123!";

    public static async Task LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/account/login");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        // DOMContentLoaded is safe here: the login page (Layout=null) has no Blazor WebSocket.
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    public static async Task LogoutAsync(IPage page)
    {
        await page.ClickAsync("button[type='submit']:has-text('Logout')");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    public static async Task CreateUserAsync(IPage page, string baseUrl,
        string email, string password, string role)
    {
        await page.GotoAsync($"{baseUrl}/admin/users");
        await page.WaitForSelectorAsync("button:has-text('Add User')");

        // Users.razor has @rendermode InteractiveServer. DOMContentLoaded fires as soon as
        // the SSR HTML arrives, but Blazor's @onclick handlers aren't active until the
        // SignalR circuit connects (~1-4 s on a local Aspire stack). A single fixed wait
        // before the first click is simpler and more reliable than a retry loop.
        await page.WaitForTimeoutAsync(4000);

        await page.ClickAsync("button:has-text('Add User')");
        await page.WaitForSelectorAsync(".card-body", new() { Timeout = 10000 });

        // Blazor InputText does not emit type="text" explicitly in .NET 10 (it relies on HTML
        // default). Select email by position (first non-password, non-hidden input in the card).
        await page.Locator(".card-body input:not([type='password']):not([type='hidden']):not([type='submit'])").First.FillAsync(email);
        await page.Locator(".card-body input[type='password']").FillAsync(password);
        await page.Locator(".card-body select").SelectOptionAsync(role);

        await page.ClickAsync(".card-body button:has-text('Create')");
        await page.WaitForSelectorAsync(".card-body",
            new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });
    }

    /// <summary>
    /// Retries selecting an option in a Blazor <c>InputSelect</c> until the selection sticks.
    /// Before the circuit connects, <c>SelectOptionAsync</c> may silently do nothing.
    /// </summary>
    public static async Task RetrySelectUntilValueAsync(
        IPage page, string selector, string value,
        int maxAttempts = 20, int intervalMs = 600)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            await page.SelectOptionAsync(selector, value);
            await page.WaitForTimeoutAsync(intervalMs);
            var selected = await page.EvalOnSelectorAsync<string>(selector, "el => el.value");
            if (selected == value) return;
        }
        throw new InvalidOperationException(
            $"Could not select '{value}' in '{selector}' after {maxAttempts} attempts.");
    }

    /// <summary>
    /// Clicks <paramref name="trigger"/> repeatedly until <paramref name="expected"/> becomes
    /// visible (or hidden when <paramref name="waitForHidden"/> is true). Handles the Blazor
    /// Interactive Server circuit warm-up gap between page load and first @onclick being active.
    /// </summary>
    private static async Task RetryClickUntilVisibleAsync(
        IPage page, string trigger, string expected,
        bool waitForHidden = false,
        int maxAttempts = 20, int intervalMs = 600)
    {
        var state = waitForHidden ? WaitForSelectorState.Hidden : WaitForSelectorState.Visible;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            await page.ClickAsync(trigger);
            try
            {
                await page.WaitForSelectorAsync(expected, new() { State = state, Timeout = intervalMs });
                return;
            }
            catch (TimeoutException)
            {
                await page.WaitForTimeoutAsync(200);
            }
        }
        // Final wait — will throw a clean Playwright timeout if the element never changes state
        await page.WaitForSelectorAsync(expected, new() { State = state, Timeout = 10000 });
    }
}
