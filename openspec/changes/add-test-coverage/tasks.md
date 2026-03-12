## 1. Solution Structure — New Test Projects

- [ ] 1.1 Create `FarmAppAspire.Tests.Unit` project: `dotnet new xunit -n FarmAppAspire.Tests.Unit -o FarmAppAspire.Tests.Unit --framework net10.0`; add to `FarmAppAspire.slnx` with `dotnet sln FarmAppAspire.slnx add FarmAppAspire.Tests.Unit/FarmAppAspire.Tests.Unit.csproj`.
- [ ] 1.2 Add packages to `FarmAppAspire.Tests.Unit.csproj`: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Moq`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `coverlet.collector`. Set `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` and `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`.
- [ ] 1.3 Add `ProjectReference` to `FarmAppAspire.Web` and `FarmAppAspire.CustomerService` in `FarmAppAspire.Tests.Unit.csproj`. Add global usings for `Xunit` and `Moq`.
- [ ] 1.4 Create `FarmAppAspire.Tests.UI` project: `dotnet new xunit -n FarmAppAspire.Tests.UI -o FarmAppAspire.Tests.UI --framework net10.0`; add to solution.
- [ ] 1.5 Add packages to `FarmAppAspire.Tests.UI.csproj`: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Microsoft.Playwright`, `Aspire.Hosting.Testing`, `coverlet.collector`. Add `ProjectReference` to `FarmAppAspire.AppHost`.
- [ ] 1.6 Add packages to existing `FarmAppAspire.Tests.csproj`: `Testcontainers.PostgreSql` and `Microsoft.AspNetCore.Mvc.Testing`. Add `ProjectReference` to `FarmAppAspire.CustomerService` and `FarmAppAspire.Web`.

## 2. Coverage Pipeline — Codecov Configuration

- [ ] 2.1 Create `codecov.yml` at the solution root defining coverage thresholds: `target: 80%` overall, and a per-flag target of `90%` for `customerservice` and `auth` flags.
- [ ] 2.2 Create `.github/workflows/ci.yml` with five jobs: `unit-tests`, `integration-tests`, `ui-tests`, `coverage-report` (needs unit + integration), and structure ready for Codecov upload step.
- [ ] 2.3 In `.github/workflows/ci.yml` — `unit-tests` job: checkout, setup .NET 10, run `dotnet test FarmAppAspire.Tests.Unit --collect:"XPlat Code Coverage" --results-directory ./coverage/unit`, upload coverage XML as artifact `coverage-unit`.
- [ ] 2.4 In `.github/workflows/ci.yml` — `integration-tests` job: checkout, setup .NET 10, run `dotnet test FarmAppAspire.Tests --collect:"XPlat Code Coverage" --results-directory ./coverage/integration`, upload coverage XML as artifact `coverage-integration`. Docker is available on `ubuntu-latest`.
- [ ] 2.5 In `.github/workflows/ci.yml` — `ui-tests` job: checkout, setup .NET 10, cache `~/.cache/ms-playwright` using `actions/cache` keyed on Playwright version, install Playwright browsers (`pwsh playwright install --with-deps chromium`), run `dotnet test FarmAppAspire.Tests.UI`.
- [ ] 2.6 In `.github/workflows/ci.yml` — `coverage-report` job (`needs: [unit-tests, integration-tests]`): download both coverage artifacts, install `dotnet-reportgenerator-globaltool`, run `reportgenerator -reports:coverage/**/coverage.cobertura.xml -targetdir:coverage/html -reporttypes:Html;Cobertura;Badges`, upload `coverage/html` as artifact `coverage-report`.
- [ ] 2.7 In `.github/workflows/ci.yml` — add `codecov/codecov-action@v4` step in `coverage-report` job: upload merged `coverage/html/Cobertura.xml`, set `fail_ci_if_error: true`, use `${{ secrets.CODECOV_TOKEN }}`.
- [ ] 2.8 Add a Codecov badge to `README.md` (or create `README.md` if absent) using the badge URL from the Codecov repository dashboard.

## 3. Unit Tests — IdentitySeeder

- [ ] 3.1 Create `FarmAppAspire.Tests.Unit/Auth/IdentitySeederTests.cs`. Set up Moq mocks for `RoleManager<IdentityRole>` and `UserManager<IdentityUser>` using the standard mock helper pattern (`new Mock<RoleManager<IdentityRole>>(Mock.Of<IRoleStore<IdentityRole>>(), ...)`).
- [ ] 3.2 Implement test: `Seeds_FourRoles_WhenNoneExist` — configure `RoleExistsAsync` to return `false` for all four roles; call seeder; verify `CreateAsync` called exactly 4 times with role names `FarmAdmin`, `Manager`, `Staff`, `ReadOnly`.
- [ ] 3.3 Implement test: `Skips_RoleCreation_WhenRolesAlreadyExist` — configure `RoleExistsAsync` to return `true`; call seeder; verify `CreateAsync` never called.
- [ ] 3.4 Implement test: `Seeds_FarmAdmin_WhenNoUsersExist` — configure `UserManager.Users` as an empty queryable; configure `IConfiguration` with test email and password; call seeder; verify `CreateAsync` called once and `AddToRoleAsync` called with `"FarmAdmin"`.
- [ ] 3.5 Implement test: `Skips_UserSeed_WhenUsersAlreadyExist` — configure `UserManager.Users` with one user; call seeder; verify `CreateAsync` never called.

## 4. Unit Tests — CustomerMappings

- [ ] 4.1 Create `FarmAppAspire.Tests.Unit/CustomerService/CustomerMappingTests.cs`.
- [ ] 4.2 Implement test: `ToSummaryDto_MapsAllFields` — create a `Customer` entity with all fields populated; assert each field on the returned `CustomerSummaryDto` matches.
- [ ] 4.3 Implement test: `ToDetailDto_SortsContacts_PrimaryFirst` — create a customer with two contacts (`IsPrimary: false` first, `IsPrimary: true` second); assert returned contacts list has primary contact at index 0.
- [ ] 4.4 Implement test: `ToDetailDto_SortsAddresses_DefaultFirst` — create a customer with two addresses (`IsDefault: false` first, `IsDefault: true` second); assert returned addresses list has default at index 0.
- [ ] 4.5 Implement test: `ToDetailDto_HandlesEmptyChildCollections` — create a customer with empty `Contacts` and `Addresses` lists; assert both collections on the returned DTO are empty (not null) and no exception is thrown.

## 5. Unit Tests — CustomerApiClientHandler

- [ ] 5.1 Create `FarmAppAspire.Tests.Unit/Web/CustomerApiClientHandlerTests.cs`. Use `Mock<IHttpContextAccessor>` to supply a `ClaimsPrincipal` and a `DelegatingHandlerStub` (or `MockHttpMessageHandler`) to capture outbound requests.
- [ ] 5.2 Implement test: `SetsXUserIdHeader_FromNameIdentifierClaim` — configure `IHttpContextAccessor` to return a user with a `ClaimTypes.NameIdentifier` claim of `"test-user-guid"`; invoke the handler; assert the captured outbound request has header `X-User-Id: test-user-guid`.
- [ ] 5.3 Implement test: `NoXUserIdHeader_WhenClaimAbsent` — configure `IHttpContextAccessor` to return a user with no `ClaimTypes.NameIdentifier` claim; invoke the handler; assert the captured outbound request does not contain the `X-User-Id` header.

## 6. Integration Tests — CustomerService Endpoints

- [ ] 6.1 Create `FarmAppAspire.Tests/CustomerService/CustomerServiceFactory.cs` implementing `WebApplicationFactory<FarmAppAspire.CustomerService.Program>` + `IAsyncLifetime`. In `InitializeAsync`, start a `PostgreSqlContainer`. In `ConfigureWebHost`, remove the existing `DbContextOptions<CustomerDbContext>` and register a new one using the container's connection string.
- [ ] 6.2 Create `FarmAppAspire.Tests/CustomerService/CustomerEndpointTests.cs` using `IClassFixture<CustomerServiceFactory>`. Add a helper method `CreateHttpClient()` that creates a client from the factory and adds `X-User-Id: test-user` header by default.
- [ ] 6.3 Implement test: `GetCustomers_ReturnsEmptyList_WhenNoneExist` — call `GET /customers`; assert HTTP 200, `total == 0`, `items` is empty array.
- [ ] 6.4 Implement test: `GetCustomers_Pagination_ReturnsCorrectSlice` — create 5 customers via `POST /customers`; call `GET /customers?page=2&size=2`; assert 2 items returned and `total == 5`.
- [ ] 6.5 Implement test: `GetCustomer_ReturnsNotFound_ForUnknownId` — call `GET /customers/{Guid.NewGuid()}`; assert HTTP 404.
- [ ] 6.6 Implement test: `GetCustomer_ReturnsDetailWithContactsAndAddresses` — create a customer, add one contact and one address; call `GET /customers/{id}`; assert 200 and nested contact and address present.
- [ ] 6.7 Implement test: `PostCustomer_CreatesWithCreatedBy_FromXUserIdHeader` — post a customer with `X-User-Id: user-abc`; assert 201 and `createdBy == "user-abc"`.
- [ ] 6.8 Implement test: `PostCustomer_Returns400_WhenXUserIdMissing` — post a customer without `X-User-Id` header; assert HTTP 400.
- [ ] 6.9 Implement test: `PostCustomer_Returns400_WhenDisplayNameEmpty` — post a customer with `DisplayName: ""`; assert HTTP 400.
- [ ] 6.10 Implement test: `PutCustomer_UpdatesModifiedBy` — create a customer; put with `X-User-Id: updater-1`; assert 200 and `modifiedBy == "updater-1"`.
- [ ] 6.11 Implement test: `PutCustomer_ReturnsNotFound_ForUnknownId` — put to unknown ID; assert 404.
- [ ] 6.12 Implement test: `DeleteCustomer_Returns204_AndCascadesChildRecords` — create a customer with one contact and one address; delete the customer; assert 204 and that subsequent `GET /customers/{id}` returns 404.
- [ ] 6.13 Implement test: `DeleteCustomer_ReturnsNotFound_ForUnknownId` — delete unknown ID; assert 404.
- [ ] 6.14 Implement test: `PostAddress_FirstAddress_AutoSetsDefault` — create a customer; post an address with `isDefault: false`; assert the returned address has `isDefault: true`.
- [ ] 6.15 Implement test: `PostAddress_NewDefault_DemotesPreviousDefault` — create a customer; post address A with `isDefault: true`; post address B with `isDefault: true`; fetch the customer; assert A has `isDefault: false` and B has `isDefault: true`.
- [ ] 6.16 Implement test: `DeleteAddress_DefaultDeleted_PromotesOldest` — create a customer with address A (created first), then address B set as default; delete B; fetch addresses; assert A is now `isDefault: true`.
- [ ] 6.17 Implement test: `DeleteAddress_NonDefaultDeleted_DefaultUnchanged` — create a customer with default address A and non-default B; delete B; fetch addresses; assert A is still `isDefault: true`.
- [ ] 6.18 Implement test: `PostContact_AddsContactToCustomer` — create a customer; post a contact; assert 201 and the contact is returned in `GET /customers/{id}/contacts`.
- [ ] 6.19 Implement test: `DeleteContact_WrongCustomerId_ReturnsNotFound` — create two customers each with a contact; delete a contact using the wrong parent customer ID; assert 404.

## 7. Integration Tests — Auth HTTP Level

- [ ] 7.1 Create `FarmAppAspire.Tests/Auth/WebApplicationFactory.cs` implementing `WebApplicationFactory<FarmAppAspire.Web.Program>` + `IAsyncLifetime`. Start a `PostgreSqlContainer`. In `ConfigureWebHost`, replace `ApplicationDbContext` options with the container connection string and inject test values for `FARM_ADMIN_EMAIL` and `FARM_ADMIN_PASSWORD` via `ConfigureAppConfiguration`. Replace Aspire Redis output cache with `AddDistributedMemoryCache`.
- [ ] 7.2 Create `FarmAppAspire.Tests/Auth/AuthIntegrationTests.cs` using `IClassFixture<WebApplicationFactory>`. Disable automatic redirect following on the HTTP client (`AllowAutoRedirect = false`) for redirect-assertion tests.
- [ ] 7.3 Implement test: `GetLoginPage_Returns200_ForUnauthenticatedRequest`.
- [ ] 7.4 Implement test: `GetHome_Redirects_UnauthenticatedUser_ToLogin` — assert response is 302 and `Location` header contains `/account/login`.
- [ ] 7.5 Implement test: `PostLogin_ValidCredentials_SetsAuthCookie` — post the seeded FarmAdmin credentials; assert response is a redirect and `Set-Cookie` header is present.
- [ ] 7.6 Implement test: `PostLogin_InvalidCredentials_NoCookieIssued` — post wrong password; assert no `Set-Cookie` header for the auth cookie.
- [ ] 7.7 Implement test: `GetAdminUsers_Returns403OrRedirect_ForStaffUser` — authenticate as a Staff user (create one via UserManager in the fixture setup); call `GET /admin/users`; assert 403 or redirect to not-authorized.
- [ ] 7.8 Implement test: `PostLogout_ClearsAuthCookieAndRedirectsToLogin` — authenticate, then post to `/account/logout`; assert redirect to `/account/login` and that the auth cookie is expired in `Set-Cookie`.

## 8. Playwright UI Tests — Fixtures and Auth Flows

- [ ] 8.1 Create `FarmAppAspire.Tests.UI/Fixtures/AspirePlaywrightFixture.cs` — starts the Aspire stack via `DistributedApplicationTestingBuilder.CreateAsync<Projects.FarmAppAspire_AppHost>()`, waits for `webfrontend` healthy, stores `BaseUrl`, initialises a headless Chromium `IBrowserContext` via Playwright. Implement as `IAssemblyFixture<AspirePlaywrightFixture>` using xUnit v3.
- [ ] 8.2 Create `FarmAppAspire.Tests.UI/Fixtures/LoginHelper.cs` — a static helper that navigates to the login page and submits credentials, returning an authenticated `IPage`.
- [ ] 8.3 Create `FarmAppAspire.Tests.UI/Auth/AuthFlowTests.cs` referencing `AspirePlaywrightFixture`.
- [ ] 8.4 Implement test: `UnauthenticatedUser_RedirectedToLogin` — navigate to `/`; assert URL ends with `/account/login`.
- [ ] 8.5 Implement test: `ValidLogin_LandsOnHome_WithUsernameInNav` — log in as FarmAdmin; assert URL is `/` and nav contains FarmAdmin email.
- [ ] 8.6 Implement test: `InvalidLogin_ShowsError_StaysOnLogin` — submit wrong password; assert page URL still contains `/account/login` and "Invalid email or password" text is visible.
- [ ] 8.7 Implement test: `Lockout_ShownAfter5FailedAttempts` — submit wrong password 5 times; assert lockout message text is visible.
- [ ] 8.8 Implement test: `Logout_ClearsSession_RedirectsToLogin` — log in, click logout; assert URL is `/account/login` and username no longer in nav.
- [ ] 8.9 Implement test: `FarmAdmin_SeesUsersNavLink` — log in as FarmAdmin; assert "Users" link is visible in nav.
- [ ] 8.10 Implement test: `StaffUser_DoesNotSeeUsersNavLink` — log in as Staff (created via a setup helper that calls the Users API or logs in as FarmAdmin first to create a Staff user); assert "Users" link is not visible.
- [ ] 8.11 Implement test: `StaffUser_AdminUsersPage_ShowsNotAuthorized` — log in as Staff; navigate to `/admin/users`; assert "not authorized" text visible and user table absent.

## 9. Playwright UI Tests — Customer Flows

- [ ] 9.1 Create `FarmAppAspire.Tests.UI/Customers/CustomerFlowTests.cs` referencing `AspirePlaywrightFixture`.
- [ ] 9.2 Implement test: `FarmAdmin_CanSeeCustomersList` — log in as FarmAdmin; navigate to `/customers`; assert page loads without error.
- [ ] 9.3 Implement test: `FarmAdmin_CanCreateCustomer_AppearsInList` — log in as FarmAdmin; navigate to `/customers/create`; fill in Wholesale customer form; submit; assert new customer name appears in the list.
- [ ] 9.4 Implement test: `Staff_CustomerListVisible_NoCreateButton` — log in as Staff; navigate to `/customers`; assert list loads and no "Create" or "New Customer" button is present.
- [ ] 9.5 Implement test: `ReadOnly_NoCreateOrEditControls` — log in as ReadOnly user; navigate to `/customers`; assert no Create, Edit, or Delete controls visible.
- [ ] 9.6 Implement test: `FarmAdmin_DeleteDefaultAddress_PromotesNextAddress` — log in as FarmAdmin; create a customer; add two addresses; delete the default one; assert the remaining address shows as default.

## 10. Verification

- [ ] 10.1 Run `dotnet test FarmAppAspire.Tests.Unit` — all unit tests pass.
- [ ] 10.2 Run `dotnet test FarmAppAspire.Tests` — all integration tests pass (existing smoke test + new auth and customer endpoint tests).
- [ ] 10.3 Run `dotnet test FarmAppAspire.Tests.UI` — all Playwright tests pass (requires Docker and running Aspire stack built locally).
- [ ] 10.4 Run coverage collection: `dotnet test FarmAppAspire.Tests.Unit --collect:"XPlat Code Coverage" --results-directory ./TestResults/unit` and `dotnet test FarmAppAspire.Tests --collect:"XPlat Code Coverage" --results-directory ./TestResults/integration`.
- [ ] 10.5 Run `reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:TestResults/CoverageReport -reporttypes:Html` and confirm HTML report opens and shows ≥ 90% line coverage for `FarmAppAspire.CustomerService` and auth files, and ≥ 80% overall.
- [ ] 10.6 Push to `master` or open a PR; confirm GitHub Actions workflow triggers all three test jobs and the coverage-report job uploads to Codecov.
- [ ] 10.7 Confirm Codecov PR comment appears with diff-coverage and that the Codecov badge in `README.md` is live.
