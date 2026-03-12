## Context

`FarmAppAspire.Web` currently authenticates users against a single account stored in `appsettings.json` (`Auth:Username`, `Auth:Password`). This was a known placeholder built in the `add-login-screen` change. The app must evolve to support multiple farm employees, each with an individual account and a role that controls their access level. All downstream services (starting with CustomerService) depend on a real `UserId` being propagated per request. This change replaces the stub auth stack entirely.

## Goals / Non-Goals

**Goals:**
- Replace config-based credentials with ASP.NET Core Identity backed by Postgres.
- Support four roles: `FarmAdmin`, `Manager`, `Staff`, `ReadOnly`.
- Provide a user management UI (FarmAdmin only) to create, edit, and deactivate accounts.
- Seed a default `FarmAdmin` account on first run so the app is never locked out.
- Expose a real `UserId` (Identity's GUID) in the cookie claims for downstream service propagation.
- Keep cookie-based auth — no move to JWT for the web app itself.

**Non-Goals:**
- No external OAuth providers (Google, Microsoft) in this phase.
- No self-service password reset or email verification — this is an internal admin-provisioned system.
- No fine-grained per-record permissions — roles apply to sections, not individual records.
- No separate Identity microservice — Identity stays inside `webfrontend`.

## Decisions

### Decision 1 — ASP.NET Core Identity over a custom user table

**Chosen**: `AddIdentity<IdentityUser, IdentityRole>` with `AddEntityFrameworkStores<IdentityDbContext>`.

**Rationale**: Identity handles password hashing (PBKDF2), account lockout, claims generation, and SignInManager lifecycle out of the box. Rebuilding these correctly is high risk with no upside for an internal app.

**Alternatives considered**:
- *Custom `Users` table with BCrypt*: Faster to write, but re-implements security-sensitive code. Rejected.
- *External identity provider (Keycloak, Auth0)*: Correct for multi-tenant SaaS, overkill for a single-org internal tool. Rejected for this phase.

---

### Decision 2 — Identity DB is its own Postgres resource in AppHost

**Chosen**: `builder.AddPostgres("identity-db")` in `AppHost.cs`, referenced only by `webfrontend`.

**Rationale**: Auth is the web frontend's concern. No other service authenticates users — they receive a trusted `X-User-Id` header. Keeping Identity isolated to `webfrontend` makes the boundary explicit and simplifies future changes (e.g., moving to an auth service later).

**Alternatives considered**:
- *Shared Postgres with CustomerService*: Saves one container but creates a logical coupling between auth and business data. Rejected.

---

### Decision 3 — Roles as Identity Roles, seeded at startup

**Chosen**: Roles (`FarmAdmin`, `Manager`, `Staff`, `ReadOnly`) are seeded via `RoleManager<IdentityRole>` on application startup using `IHostedService` or `WebApplication` startup hook. A default `FarmAdmin` account is seeded from environment config if no users exist.

**Rationale**: Seeding on startup means the app is always in a consistent state without requiring a separate migration step. The default admin credentials come from `FARM_ADMIN_EMAIL` and `FARM_ADMIN_PASSWORD` env vars (or `appsettings.Development.json`), never hardcoded.

---

### Decision 4 — Login page stays as a Razor Page, updated to use SignInManager

**Chosen**: `Login.cshtml.cs` replaces the `IConfiguration` credential check with `SignInManager<IdentityUser>.PasswordSignInAsync`. Page shape (username field, password field, return URL) stays the same.

**Rationale**: The Razor Page approach for cookie auth is correct (HTTP-level cookie write, no Blazor SignalR involved). No structural change needed — only the credential validation logic changes.

---

### Decision 5 — UserId in cookie claim for downstream propagation

**Chosen**: After `SignInAsync`, add `ClaimTypes.NameIdentifier` = `user.Id` (Identity's GUID) to the claims principal. The typed HTTP client in the Web project reads this claim and forwards it as the `X-User-Id` header to CustomerService.

**Rationale**: Using Identity's GUID (rather than username) as the user identifier is stable — a username can change, an ID cannot. This is the anchor all downstream audit records will use.

## Risks / Trade-offs

- **[Risk] Postgres must be healthy before Identity migrations run** → Mitigation: `WaitFor(identityDb)` in AppHost; use EF Core's `MigrateAsync()` at startup with retry logic from ServiceDefaults' resilience handler.
- **[Risk] Default admin seed account exposed in config** → Mitigation: `FARM_ADMIN_PASSWORD` must be set as an environment variable or Aspire secret in non-development environments; `appsettings.json` only carries a placeholder.
- **[Risk] Existing `Auth:Username`/`Auth:Password` config keys are removed** → This is a breaking change. Any environment variable or secret store using those keys must be updated. Document clearly in migration notes.
- **[Risk] Identity adds ~8 tables to the database** → Acceptable for a Postgres-backed app. Schema is well-known and well-supported.
- **[Risk] User management UI is FarmAdmin-only** → If the seed fails or the FarmAdmin password is lost, the system is locked out. Mitigation: seed can be re-triggered by clearing the Users table, documented in runbook.

## Migration Plan

1. Add `identity-db` Postgres resource in `AppHost.cs`.
2. Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL` to `FarmAppAspire.Web.csproj`.
3. Create `ApplicationDbContext : IdentityDbContext`, register with `AddDbContext`.
4. Replace `AddAuthentication(Cookie).AddCookie(...)` with `AddIdentity<IdentityUser, IdentityRole>(...).AddEntityFrameworkStores<ApplicationDbContext>()`.
5. Create and run EF Core migrations for Identity schema.
6. Update `Login.cshtml.cs` to use `SignInManager`.
7. Implement startup seeder for roles and default FarmAdmin account.
8. Build `/admin/users` Blazor page.
9. Remove `Auth:Username` / `Auth:Password` from all `appsettings*.json` files.
10. Add `[Authorize(Roles = "...")]` to pages requiring role restrictions.

**Rollback**: Revert to previous commit. The `add-login-screen` change is the last stable auth state.

## Open Questions

- Should `username` be an email address or a display name (e.g., "john.smith")? Email is the Identity default and supports future password-reset flows — recommended.
- Should the user management page support password reset by admin (generate a temp password)? Likely yes — add to scope if straightforward.
