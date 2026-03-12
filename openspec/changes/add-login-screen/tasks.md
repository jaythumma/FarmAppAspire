## 1. Authentication Middleware Setup

- [x] 1.1 In `FarmAppAspire.Web/Program.cs`, add `builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { ... })` with `HttpOnly`, `Secure`, `SameSite=Strict`, and `LoginPath = "/account/login"` configured.
- [x] 1.2 In `FarmAppAspire.Web/Program.cs`, add `builder.Services.AddAuthorization()` with a fallback policy requiring authenticated users.
- [x] 1.3 Add `app.UseAuthentication()` and `app.UseAuthorization()` to the middleware pipeline, ordered after `UseRouting` and before `MapBlazorHub` / `MapRazorPages`.

## 2. Configuration

- [x] 2.1 Add `Auth:Username` and `Auth:Password` entries to `FarmAppAspire.Web/appsettings.json` with placeholder development values.
- [x] 2.2 Add the same keys to `FarmAppAspire.Web/appsettings.Development.json` (or use user secrets) with a known test account.

## 3. Login Razor Page

- [x] 3.1 Create `FarmAppAspire.Web/Pages/Account/Login.cshtml` and `Login.cshtml.cs` — enable Razor Pages with `builder.Services.AddRazorPages()` and `app.MapRazorPages()` in `Program.cs` if not already present.
- [x] 3.2 Implement `OnGetAsync` to render the form with an optional `returnUrl` query parameter; redirect to `/` if already authenticated.
- [x] 3.3 Implement `OnPostAsync` to validate credentials against `IConfiguration`, call `HttpContext.SignInAsync`, and use `LocalRedirect(returnUrl ?? "/")` on success.
- [x] 3.4 On failed login, add a `ModelState` error ("Invalid username or password") and re-render the form without issuing a cookie.
- [x] 3.5 Add model-level `[Required]` validation to `Username` and `Password` input properties; display validation summary in the Razor Page markup.

## 4. Logout Endpoint

- [x] 4.1 In `Program.cs`, add `app.MapPost("/account/logout", async (HttpContext ctx) => { await ctx.SignOutAsync(...); return Results.Redirect("/account/login"); })` — no `[Authorize]` required (graceful for unauthenticated calls).

## 5. Route Protection (Blazor)

- [x] 5.1 In `FarmAppAspire.Web/Components/App.razor` (or `Routes.razor`), replace `<RouteView>` with `<AuthorizeRouteView>`. Add a `<NotAuthorized>` fragment that calls `NavigationManager.NavigateTo($"/account/login?returnUrl={Uri.EscapeDataString(navigationManager.Uri)}", forceLoad: true)`.
- [x] 5.2 Wrap the router with `<CascadingAuthenticationState>` if not already present.

## 6. Navigation — Logout Link

- [x] 6.1 Inject `AuthenticationStateProvider` (or use `<AuthorizeView>`) in `NavMenu.razor`.
- [x] 6.2 Inside an `<AuthorizeView>` `<Authorized>` block, display the username (`context.User.Identity?.Name`) and a logout `<form>` that POSTs to `/account/logout` (use a `<button type="submit">` inside a `<form method="post" action="/account/logout">`; include the anti-forgery token via `AntiForgeryToken()`).

## 7. Verification

- [ ] 7.1 Run the app via `dotnet run --project FarmAppAspire.AppHost` and confirm unauthenticated navigation to `/` redirects to `/account/login`.
- [ ] 7.2 Log in with the configured test credentials and confirm redirect back to the original page.
- [ ] 7.3 Confirm the logout link appears in the nav after login and that clicking it clears the session.
- [ ] 7.4 Confirm submitting bad credentials shows a validation error and does not log in.
- [ ] 7.5 Run `dotnet test FarmAppAspire.Tests` and confirm existing integration tests still pass.
