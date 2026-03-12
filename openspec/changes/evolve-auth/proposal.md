## Why

The current login system authenticates against a single hardcoded account in `appsettings.json`. FarmAppAspire is a multi-user internal business application where farm employees need individual accounts and role-based access to different parts of the system. This must be in place before the CustomerService is built, as CustomerService records must be audited per user and access controlled by role.

## What Changes

- **BREAKING**: Replace the single config-based credential with ASP.NET Core Identity backed by a dedicated Postgres database.
- Add four farm-specific roles: `FarmAdmin`, `Manager`, `Staff`, `ReadOnly`.
- Introduce a user management UI (FarmAdmin only) for creating, editing, and deactivating accounts.
- Update the login page to authenticate via `SignInManager<IdentityUser>` instead of `IConfiguration`.
- Update route protection to support role-based access in addition to authentication checks.
- Add `identity-db` Postgres resource to `AppHost.cs` and wire it to `webfrontend`.
- Seed a default `FarmAdmin` account on first run so the app is not locked out.

## Capabilities

### New Capabilities

- `identity-persistence`: ASP.NET Core Identity wired to a Postgres database (users, roles, password hashing, lockout). Replaces the config-based credential entirely.
- `user-management`: A `/admin/users` section (FarmAdmin only) for inviting/creating users, assigning roles, and deactivating accounts.
- `farm-roles`: The four role definitions (`FarmAdmin`, `Manager`, `Staff`, `ReadOnly`) and the permission matrix that controls access to each section of the app.

### Modified Capabilities

- `user-login`: Login requirement now means authenticating any registered Identity user, not just a single config account. The credential source and validation mechanism change entirely.
- `route-protection`: In addition to requiring authentication, protected routes must now enforce role-based access — certain pages are restricted to specific roles.

## Impact

- `FarmAppAspire.Web/Program.cs` — replace `AddAuthentication/AddCookie` with `AddIdentity`, add `AddDbContext` for Identity, update authorization policies.
- `FarmAppAspire.Web/Pages/Account/Login.cshtml.cs` — replace `IConfiguration` credential check with `SignInManager<IdentityUser>.PasswordSignInAsync`.
- `FarmAppAspire.Web/Components/Routes.razor` — `AuthorizeRouteView` already in place; role checks added per protected page.
- `FarmAppAspire.Web/Components/Layout/NavMenu.razor` — role-conditional nav links.
- `FarmAppAspire.AppHost/AppHost.cs` — add `AddPostgres("identity-db")`, reference from `webfrontend`.
- New NuGet packages: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`.
- `appsettings.json` — `Auth:Username` / `Auth:Password` entries removed (**BREAKING** for any environment relying on them).
