## Why

The three completed changes (`add-login-screen`, `evolve-auth`, `add-customer-service`) have left the codebase with a single integration smoke test and zero automated coverage of auth logic, CustomerService endpoints, or Blazor UI flows. Before adding further business capabilities, a structured test suite must be in place to prevent regressions, validate existing behaviour, and enforce the 90% coverage target on critical paths.

## What Changes

- Add `FarmAppAspire.Tests.Unit` project — pure unit tests for `IdentitySeeder`, `CustomerMappings`, and `CustomerApiClientHandler` using xUnit v3 + Moq, no infrastructure dependencies.
- Add `FarmAppAspire.Tests.UI` project — Playwright E2E tests for auth flows and customer CRUD flows in a real browser against the full Aspire stack.
- Extend `FarmAppAspire.Tests` (existing integration project) — add endpoint-level tests for all CustomerService routes using `WebApplicationFactory` + `Testcontainers.PostgreSql`, and HTTP-level auth tests.
- Add `.github/workflows/ci.yml` — GitHub Actions workflow that runs all three test projects, merges coverage, uploads to Codecov, and gates on coverage thresholds (90% critical, 80% overall).
- Integrate ReportGenerator for local HTML coverage reports and Codecov for GitHub PR diff-coverage comments and trend visualisation.

## Capabilities

### New Capabilities

- `unit-tests`: Unit tests covering `IdentitySeeder` seed logic, `CustomerMappings` DTO transformations, and `CustomerApiClientHandler` header propagation using Moq — no infrastructure required.
- `customer-endpoint-tests`: Integration tests for all CustomerService REST endpoints (`/customers`, contacts, addresses) using `WebApplicationFactory<Program>` + `Testcontainers.PostgreSql`, covering business rules including default-address promotion and the `X-User-Id` filter.
- `auth-integration-tests`: HTTP-level integration tests for the ASP.NET Core Identity login/logout flow — cookie issuance, bad credential handling, route protection redirect, and role-based access denial.
- `playwright-ui-tests`: Playwright E2E tests in a dedicated project that start the full Aspire stack via `Aspire.Hosting.Testing` and validate auth flows, role-conditional nav visibility, and customer CRUD in a real browser.
- `coverage-pipeline`: GitHub Actions CI workflow (`.github/workflows/ci.yml`) that runs all test projects, merges Cobertura XML coverage with ReportGenerator, uploads to Codecov, and fails the build if coverage drops below thresholds.

### Modified Capabilities

## Impact

- New project `FarmAppAspire.Tests.Unit` added to solution (`FarmAppAspire.slnx`).
- New project `FarmAppAspire.Tests.UI` added to solution.
- `FarmAppAspire.Tests.csproj` — add `Testcontainers.PostgreSql` and `Microsoft.AspNetCore.Mvc.Testing`; add `ProjectReference` to `FarmAppAspire.Web` and `FarmAppAspire.CustomerService`.
- `FarmAppAspire.Tests.Unit.csproj` — references `FarmAppAspire.Web` and `FarmAppAspire.CustomerService`; packages: `Moq`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `coverlet.collector`.
- `FarmAppAspire.Tests.UI.csproj` — references `FarmAppAspire.AppHost`; packages: `Microsoft.Playwright`, `Aspire.Hosting.Testing`, `coverlet.collector`.
- `.github/workflows/ci.yml` — new file; requires `CODECOV_TOKEN` GitHub secret.
- No changes to production application code.
