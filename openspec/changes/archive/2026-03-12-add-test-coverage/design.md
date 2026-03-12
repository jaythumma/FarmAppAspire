## Context

The codebase has three implemented features — cookie auth, ASP.NET Core Identity with farm roles, and a CustomerService with full CRUD — but only a single integration smoke test (`GetWebResourceRootReturnsOkStatusCode`) that verifies the web frontend returns 200. No tests exist for IdentitySeeder logic, CustomerService endpoint behaviour, role-based access enforcement, or any Blazor UI flows.

The existing `FarmAppAspire.Tests` project uses `Aspire.Hosting.Testing` to spin up the full Aspire graph and is the correct home for smoke and end-to-end integration tests. Docker is available in CI.

## Goals / Non-Goals

**Goals:**
- Achieve ≥ 90% line/branch coverage on `FarmAppAspire.CustomerService` and auth-related code in `FarmAppAspire.Web` (IdentitySeeder, CustomerApiClientHandler, Login page handler).
- Achieve ≥ 80% overall combined coverage across the solution.
- Automate all manual verification tasks listed in the `evolve-auth` and `add-customer-service` change task lists.
- Produce visual coverage reports locally (HTML) and on every PR (Codecov diff annotations).
- Keep test execution fast enough for CI — unit and integration test suites should complete in under 3 minutes; UI tests can run separately or in parallel.

**Non-Goals:**
- Test coverage of generated EF Core migrations.
- Coverage of static Blazor SSR pages via in-process instrumentation (Playwright validates behaviour; coverage numbers come from unit and integration tests).
- Load or performance testing.
- Testing third-party packages (Identity, EF Core internals).

## Decisions

---

### Decision 1 — Three test projects: Unit, Integration (existing), UI

**Chosen**: Split into three projects:
1. `FarmAppAspire.Tests.Unit` — pure xUnit + Moq, no infrastructure.
2. `FarmAppAspire.Tests` (existing) — extended with Testcontainers endpoint tests and HTTP auth tests.
3. `FarmAppAspire.Tests.UI` — Playwright E2E, separate project, runs full Aspire stack.

**Rationale**: Separating by speed and dependency profile lets CI run fast feedback (unit) before slow infra tests (integration → UI), and prevents Playwright's browser dependencies from polluting the integration project. The existing `FarmAppAspire.Tests` already has `Aspire.Hosting.Testing` wired correctly — extending it avoids duplication of the AppHost project reference pattern.

**Alternatives considered**:
- Single test project: rejected — Playwright's `pwsh playwright install` step and the Aspire.Hosting.Testing slow startup would affect all test runs.
- Separate integration project for auth vs. customer: rejected — over-splitting for the scale of this codebase.

---

### Decision 2 — CustomerService endpoint tests use WebApplicationFactory + Testcontainers (not Aspire.Hosting.Testing)

**Chosen**: `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing` with `Testcontainers.PostgreSql` to replace the Aspire-provided Postgres connection string.

**Rationale**: The CustomerService is a standalone minimal API with no Aspire-specific runtime behaviour beyond the Postgres connection string. Using WebApplicationFactory gives fast in-process HTTP testing (no Docker network overhead for the HTTP layer), while Testcontainers provides a real Postgres so EF Core migrations and constraint validation behave correctly. The full Aspire stack startup (30–60s) is too slow for 18+ focused endpoint tests.

**Alternatives considered**:
- EF Core `UseInMemoryDatabase`: rejected — InMemory doesn't enforce FK constraints or the default-address promotion logic that uses `ExecuteUpdateAsync` (which requires a real DB).
- Full Aspire.Hosting.Testing for all tests: rejected — startup time dominates; acceptable only for smoke tests.

**Implementation pattern**:
```csharp
public class CustomerServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public new async Task DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CustomerDbContext>>();
            services.AddDbContext<CustomerDbContext>(o =>
                o.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
```
Migrations run at CustomerService startup via the existing `MigrateAsync()` call in `Program.cs`.

---

### Decision 3 — Auth integration tests use WebApplicationFactory + Testcontainers on FarmAppAspire.Web

**Chosen**: `WebApplicationFactory<Program>` targeting `FarmAppAspire.Web` with `Testcontainers.PostgreSql` replacing both `identity-db` and disabling the Redis output cache. The `IdentitySeeder` hosted service runs at startup within the factory, seeding the FarmAdmin account and 4 roles so tests can authenticate immediately.

**Rationale**: Auth behaviour (cookie issuance, login redirect, lockout) is HTTP-level and testable in-process. Running the full Aspire graph for cookie sign-in tests is unnecessary. The seeder's idempotency means a freshly migrated test DB always starts with known state.

**Key override**:
```csharp
builder.ConfigureServices(services =>
{
    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
    services.AddDbContext<ApplicationDbContext>(o =>
        o.UseNpgsql(_postgres.GetConnectionString()));
    // Replace Aspire Redis cache with in-memory
    services.AddDistributedMemoryCache();
});
```

---

### Decision 4 — Playwright tests start the full Aspire stack via Aspire.Hosting.Testing

**Chosen**: `FarmAppAspire.Tests.UI` uses `DistributedApplicationTestingBuilder.CreateAsync<Projects.FarmAppAspire_AppHost>()` to start the complete Aspire resource graph (identity-db, customer-db, Redis, apiservice, customerservice, webfrontend) once per test assembly via `IAssemblyFixture`.

**Rationale**: Playwright drives a real browser against a real server — no shortcuts. The Aspire.Hosting.Testing fixture ensures all services and databases are healthy before any test runs. The slow startup (30–60s) is acceptable because UI tests run once per CI job, not on every test class.

**Assembly-level fixture pattern**:
```csharp
public class AspirePlaywrightFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    public string BaseUrl { get; private set; } = "";
    public IBrowserContext BrowserContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.FarmAppAspire_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("webfrontend");
        BaseUrl = _app.GetEndpoint("webfrontend").AbsoluteUri;
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(
            new() { Headless = true });
        BrowserContext = await browser.NewContextAsync();
    }
}
```

---

### Decision 5 — Playwright tests do NOT contribute to Coverlet coverage numbers

**Chosen**: Accept that Playwright test runs do not increment the coverage report. Coverage targets (90%/80%) are met by unit + integration tests alone.

**Rationale**: Coverlet instruments the test runner process. When Playwright drives a browser against a running Aspire server, the server is a separate OS process — Coverlet in the test runner has no visibility into server-side execution. Options like `dotnet-coverage collect` around the server process exist but add significant CI complexity. Playwright's value is behavioral validation and verification-task automation, not coverage numbers.

---

### Decision 6 — Coverage tooling: Coverlet → ReportGenerator (HTML) → Codecov (GitHub)

**Chosen**:
- **Coverlet** (`coverlet.collector`) — already in `FarmAppAspire.Tests`; added to Unit and UI projects. Outputs `coverage.cobertura.xml` per project.
- **ReportGenerator** (`dotnet-reportgenerator-globaltool`) — merges all Cobertura XMLs into a single HTML report with per-file heat maps, branch/line/method drill-down, and historical trend charts. Installed as a dotnet global tool in CI.
- **Codecov** — GitHub App integration via `codecov/codecov-action`. Provides PR diff-coverage annotations, per-PR coverage delta, trend dashboard, and a README badge. Requires a `CODECOV_TOKEN` GitHub secret.

**Local developer workflow**:
```sh
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:TestResults/**/coverage.cobertura.xml \
                -targetdir:TestResults/CoverageReport \
                -reporttypes:Html
```
Open `TestResults/CoverageReport/index.html` for the visual report.

**Alternatives considered**:
- JetBrains dotCover: rejected — requires JetBrains licence, not free in CI.
- `dotnet-coverage` (Microsoft): newer tool, less mature ecosystem integration with Codecov.
- Coveralls: similar to Codecov but Codecov has better diff-coverage UX and is more widely used in .NET OSS.

---

### Decision 7 — Coverage thresholds enforced in CI, not in MSBuild

**Chosen**: GitHub Actions workflow step fails if Codecov reports combined coverage below 80% overall or below 90% for the `FarmAppAspire.CustomerService` and auth-related files. Threshold enforcement is done via Codecov's `fail_ci_if_error` and a `codecov.yml` configuration file, not via MSBuild `<CoverageThreshold>` properties.

**Rationale**: MSBuild thresholds only check per-project coverage and block local builds, which creates friction during incremental development. Codecov thresholds fire only in CI and are visible in PR comments, which is the right gate.

## Risks / Trade-offs

- **[Risk] Testcontainers requires Docker in CI** → Mitigation: Docker is confirmed available in the CI environment. `Testcontainers.PostgreSql` pulls `postgres:latest` on first run; pin to a specific tag (e.g., `postgres:16`) to avoid unexpected upgrades.
- **[Risk] Aspire.Hosting.Testing in UI tests may time out on slow CI runners** → Mitigation: Set a generous `WaitForResourceHealthyAsync` timeout (120s). The Aspire test builder supports `--no-build` to speed up subsequent runs locally.
- **[Risk] WebApplicationFactory for Web project requires disabling Aspire service discovery** → Mitigation: Override connection strings via Testcontainers in `ConfigureWebHost`; disable `WithExternalHttpEndpoints` behaviour by not wiring Aspire discovery. `AddServiceDefaults()` is safe to call — it gracefully no-ops service discovery when not running under Aspire.
- **[Risk] IdentitySeeder reads env vars `FARM_ADMIN_EMAIL` / `FARM_ADMIN_PASSWORD`** → Mitigation: In `WebApplicationFactory`, configure these via `builder.ConfigureAppConfiguration` to inject test-only values; tests must not depend on local developer `appsettings.Development.json`.
- **[Risk] Playwright `pwsh playwright install` step adds ~200MB of browser binaries to CI** → Mitigation: Cache `~/.cache/ms-playwright` in GitHub Actions using `actions/cache`. Install only `chromium`, not the full suite.
- **[Risk] Blazor Interactive Server pages use SignalR; Playwright may see partially rendered state** → Mitigation: Use `WaitForSelectorAsync` / `page.WaitForLoadStateAsync(LoadState.NetworkIdle)` before assertions in Playwright tests.
