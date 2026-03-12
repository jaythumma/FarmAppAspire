## Why

The FarmAppAspire web frontend is currently open to anyone who can reach it. Adding a login screen protects the application so that only authenticated users can access farm data and features.

## What Changes

- Add a login page (`/login`) to the Blazor Server frontend where users enter credentials.
- Protect all existing routes so unauthenticated users are redirected to `/login`.
- Add a logout action that clears the session and returns users to `/login`.
- Wire up ASP.NET Core cookie-based authentication in `FarmAppAspire.Web`.

## Capabilities

### New Capabilities

- `user-login`: Login page UI, credential validation, cookie issuance, and redirect-after-login flow.
- `route-protection`: Authorization policy and route guard that redirects unauthenticated requests to `/login`.
- `user-logout`: Logout endpoint that revokes the authentication cookie and redirects to `/login`.

### Modified Capabilities

<!-- No existing specs have requirement changes. -->

## Impact

- `FarmAppAspire.Web/Program.cs` – add `AddAuthentication` / `AddAuthorization`, cookie middleware, and a `/logout` endpoint.
- `FarmAppAspire.Web/Components/` – new `Login.razor` page; `Routes.razor` or `App.razor` updated with `AuthorizeRouteView`.
- `FarmAppAspire.Web/Components/Layout/` – add user identity display and logout link to `NavMenu` / `MainLayout`.
- No changes to `FarmAppAspire.ApiService` or `FarmAppAspire.AppHost` are required for this phase.
- New NuGet dependency: `Microsoft.AspNetCore.Authentication.Cookies` (ships in-box with .NET 10, no extra package needed).
