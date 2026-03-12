namespace FarmAppAspire.Tests.UI.Fixtures;

public static class LoginHelper
{
    public const string AdminEmail = "admin@farm.local";
    public const string AdminPassword = "Farm@dev123!";

    public static async Task LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/account/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public static async Task LogoutAsync(IPage page)
    {
        await page.ClickAsync("button[type='submit']:has-text('Logout')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public static async Task CreateUserAsync(IPage page, string baseUrl,
        string email, string password, string role)
    {
        await page.GotoAsync($"{baseUrl}/admin/users");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.ClickAsync("button:has-text('Add User')");
        await page.WaitForSelectorAsync("text=New User");

        await page.FillAsync("label:has-text('Email') + input, .card input[type='text']:first-of-type", email);
        await page.FillAsync("label:has-text('Password') + input, .card input[type='password']", password);
        await page.SelectOptionAsync("label:has-text('Role') + select, .card select", role);
        await page.ClickAsync("button:has-text('Create')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
