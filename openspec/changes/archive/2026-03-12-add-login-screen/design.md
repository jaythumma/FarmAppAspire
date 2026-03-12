## Context

`FarmAppAspire.Web` is a Blazor Server app running on .NET 10. Currently there is no authentication; all routes are publicly accessible. The frontend communicates with `FarmAppAspire.ApiService` via a typed `HttpClient` registered with Aspire service discovery. ASP.NET Core's built-in cookie authentication middleware ships in the framework — no extra NuGet packages are needed.

## Goals / Non-Goals

**Goals:**
- Protect all Blazor routes so unauthenticated users are redirected to a login page.
- Provide a login form that validates credentials and issues an authentication cookie.
- Provide a logout mechanism that revokes the cookie and redirects to login.
- Keep the implementation self-contained in `FarmAppAspire.Web` with no API-service changes.

**Non-Goals:**
- No persistent user store (database, Identity, LDAP). A single hardcoded test account is acceptable for this phase.
- No OAuth / OpenID Connect external providers in this phase.
- No role-based authorization beyond "authenticated vs. anonymous".
- No changes to `FarmAppAspire.ApiService` or `FarmAppAspire.AppHost`.

## Decisions

### Decision 1 – Use a Razor Page for the login form, not a Blazor component

**Chosen**: `Pages/Account/Login.cshtml` Razor Page handles the `GET` (render form) and `POST` (validate + `SignInAsync` + redirect).

**Rationale**: ASP.NET Core authentication cookies must be written on an HTTP response. In Blazor Server, the SignalR connection is established after the initial HTTP response, so a Blazor component cannot write a Set-Cookie header mid-session. A Razor Page runs over plain HTTP, making cookie issuance straightforward.

**Alternatives considered**:
- *Blazor component calling a `/account/login` minimal API endpoint*: Works but requires a full page reload (`NavigationManager.NavigateTo(url, forceLoad: true)`) and is harder to keep secure (CSRF).
- *ASP.NET Core Identity*: Correct long-term choice but overkill for this phase; adds database migrations and significant scaffolding.

---

### Decision 2 – `AuthorizeRouteView` with `RedirectToLogin` component

**Chosen**: Replace `RouteView` with `AuthorizeRouteView` in `Routes.razor`. Supply a `<NotAuthorized>` fragment that uses `NavigationManager` to force-navigate to `/account/login?returnUrl=...`.

**Rationale**: Native Blazor primitive; automatically re-evaluates when the `AuthenticationState` changes.

---

### Decision 3 – Single hardcoded account via `IConfiguration`

**Chosen**: Credentials read from `appsettings.json` (`Auth:Username` / `Auth:Password`). Validated in the Razor Page `OnPostAsync`.

**Rationale**: Avoids a database for this phase while still being configurable per environment via environment variables or Aspire-managed secrets.

---

### Decision 4 – Logout via minimal API `POST /account/logout`

**Chosen**: Add a `app.MapPost("/account/logout", ...)` endpoint in `Program.cs` that calls `SignOutAsync` and redirects to `/account/login`.

**Rationale**: Same cookie-write constraint as login; a tiny minimal API endpoint is simpler than adding a second Razor Page.

## Risks / Trade-offs

- **Hardcoded single account** → Not production-ready. Mitigation: document clearly as a dev/demo credential; replace with Identity before any real deployment.
- **Cookie security defaults** → Must set `HttpOnly`, `Secure`, and `SameSite=Strict`. Mitigation: configure `CookieAuthenticationOptions` explicitly in `Program.cs`.
- **Return-URL open redirect** → `/account/login?returnUrl=` must validate that the URL is local before redirecting. Mitigation: use `LocalRedirect` (throws on non-local URLs) in the Razor Page.
- **Blazor pre-rendering** → During SSR pre-render the `AuthenticationState` is available; interactive re-render must not flash unauthenticated content. Mitigation: `AuthorizeRouteView` handles this correctly out of the box with `StreamRendering` disabled on the login page.
