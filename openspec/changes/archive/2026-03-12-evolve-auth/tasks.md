## 1. Infrastructure & Dependencies

- [x] 1.1 Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet packages to `FarmAppAspire.Web.csproj`.
- [x] 1.2 Add `Microsoft.EntityFrameworkCore.Design` to `FarmAppAspire.Web.csproj` (for `dotnet ef` tooling).
- [x] 1.3 In `AppHost.cs`, add `builder.AddPostgres("identity-db")` and update `webfrontend` to `WithReference(identityDb).WaitFor(identityDb)`.

## 2. Identity DbContext & EF Core Setup

- [x] 2.1 Create `FarmAppAspire.Web/Data/ApplicationDbContext.cs` extending `IdentityDbContext`.
- [x] 2.2 Register `ApplicationDbContext` in `Program.cs` with `AddNpgsqlDbContext<ApplicationDbContext>` using the Aspire connection name `"identity-db"`.
- [x] 2.3 Run `dotnet ef migrations add InitialIdentitySchema` targeting `FarmAppAspire.Web` to generate the Identity schema migration.
- [x] 2.4 Add a startup call to `dbContext.Database.MigrateAsync()` (via `IHostedService` or `WebApplication` startup hook) so migrations apply automatically before requests are served.

## 3. Replace Authentication & Authorization Registration

- [x] 3.1 In `Program.cs`, remove `builder.Services.AddAuthentication(Cookie).AddCookie(...)` and replace with `builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => { ... }).AddEntityFrameworkStores<ApplicationDbContext>()`.
- [x] 3.2 Configure Identity options: require confirmed account = false, lockout threshold = 5 attempts, lockout duration = 15 minutes.
- [x] 3.3 Configure the Identity cookie options (retain `HttpOnly`, `Secure`, `SameSite=Strict`, `LoginPath = "/account/login"`).
- [x] 3.4 Update `builder.Services.AddAuthorization()` — retain `.RequireAuthorization()` on `MapRazorComponents`.

## 4. Role & Admin Account Seeder

- [x] 4.1 Create `FarmAppAspire.Web/Data/IdentitySeeder.cs` — an `IHostedService` (or startup extension) that seeds the four roles (`FarmAdmin`, `Manager`, `Staff`, `ReadOnly`) using `RoleManager<IdentityRole>`.
- [x] 4.2 In `IdentitySeeder`, if no users exist, create a default FarmAdmin from `FARM_ADMIN_EMAIL` and `FARM_ADMIN_PASSWORD` env vars (fallback to `appsettings.Development.json` values).
- [x] 4.3 Register `IdentitySeeder` in `Program.cs` and ensure it runs after `MigrateAsync` completes.
- [x] 4.4 Add `FARM_ADMIN_EMAIL` and `FARM_ADMIN_PASSWORD` entries to `appsettings.Development.json` with dev-only placeholder values; add entries (no values) to `appsettings.json` as documentation.

## 5. Update Login Page

- [x] 5.1 Inject `SignInManager<IdentityUser>` and `UserManager<IdentityUser>` into `Login.cshtml.cs`.
- [x] 5.2 Replace the `IConfiguration` credential check in `OnPostAsync` with `SignInManager.PasswordSignInAsync(Username, Password, isPersistent: false, lockoutOnFailure: true)`.
- [x] 5.3 Handle `SignInResult`: success → `LocalRedirect`; failed → "Invalid email or password" error; locked out → "Account is locked" error.
- [x] 5.4 After successful sign-in, add `ClaimTypes.NameIdentifier` = `user.Id` to the claims principal so downstream services receive the Identity GUID.
- [x] 5.5 Update the login form label from "Username" to "Email" and set `autocomplete="email"` on the input.
- [x] 5.6 Remove `Auth:Username` and `Auth:Password` keys from `appsettings.json` and `appsettings.Development.json`.

## 6. Route Protection — Role Enforcement

- [x] 6.1 Update `Routes.razor` `<NotAuthorized>` fragment: if user is authenticated (wrong role), render an inline `<NotAuthorizedView />` component; if unauthenticated, keep the `<RedirectToLogin />` behavior.
- [x] 6.2 Create `FarmAppAspire.Web/Components/NotAuthorizedView.razor` — a simple "You are not authorized to view this page" message within the standard layout.
- [x] 6.3 Add role-conditional rendering in `NavMenu.razor` using `<AuthorizeView Roles="FarmAdmin">` blocks to hide admin navigation from non-admin users.

## 7. User Management UI

- [x] 7.1 Create `FarmAppAspire.Web/Components/Pages/Admin/Users.razor` with `@attribute [Authorize(Roles = "FarmAdmin")]` and `@rendermode InteractiveServer`.
- [x] 7.2 Implement the user list: fetch all users from `UserManager`, display email, display name, role, and active status in a table.
- [x] 7.3 Implement "Create User" form (inline or modal): email, display name, role selector, calls `UserManager.CreateAsync` + `UserManager.AddToRoleAsync`.
- [x] 7.4 Implement "Edit Role" action: role dropdown per row, calls `UserManager.RemoveFromRolesAsync` + `UserManager.AddToRoleAsync`; disable for the current user's own row.
- [x] 7.5 Implement "Deactivate / Reactivate" toggle: set `LockoutEnabled = true` and `LockoutEnd = DateTimeOffset.MaxValue` to deactivate; clear `LockoutEnd` to reactivate. Disable for current user's own row.
- [x] 7.6 Implement "Reset Password" action: generate a temp password with `UserManager.GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`, display the temp password once in a dismissible alert.
- [x] 7.7 Add `/admin/users` nav link inside an `<AuthorizeView Roles="FarmAdmin">` block in `NavMenu.razor`.

## 8. Verification

- [ ] 8.1 Run the app; confirm migration runs on startup and Identity tables are created in `identity-db`.
- [ ] 8.2 Confirm seed creates the FarmAdmin account and four roles on first run, and is skipped on subsequent runs.
- [ ] 8.3 Log in with the seeded FarmAdmin account; confirm redirect to `/` and that the username appears in the nav.
- [ ] 8.4 Log in as a `Staff` user (created via user management); confirm `/admin/users` shows "not authorized".
- [ ] 8.5 Test account lockout: submit wrong password 6 times, confirm lockout message appears.
- [ ] 8.6 Test deactivated account: deactivate a user, confirm login is rejected with "inactive" message.
- [ ] 8.7 Run `dotnet test FarmAppAspire.Tests` and confirm existing integration tests still pass.
