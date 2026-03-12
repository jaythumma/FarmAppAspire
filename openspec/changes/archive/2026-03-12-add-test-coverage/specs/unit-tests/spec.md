## ADDED Requirements

### Requirement: IdentitySeeder seeds roles on first run
The `IdentitySeeder` hosted service SHALL create all four farm roles (`FarmAdmin`, `Manager`, `Staff`, `ReadOnly`) via `RoleManager<IdentityRole>` on application startup if they do not already exist.

#### Scenario: Roles created when none exist
- **WHEN** `IdentitySeeder.StartAsync` is called against an empty Identity database
- **THEN** exactly four roles are created: `FarmAdmin`, `Manager`, `Staff`, `ReadOnly`

#### Scenario: Seeder is idempotent for roles
- **WHEN** `IdentitySeeder.StartAsync` is called and all four roles already exist
- **THEN** no duplicate roles are created and no exception is thrown

### Requirement: IdentitySeeder seeds default FarmAdmin on first run
The `IdentitySeeder` SHALL create a default `FarmAdmin` user from `FARM_ADMIN_EMAIL` and `FARM_ADMIN_PASSWORD` configuration values when no users exist in the Identity store.

#### Scenario: FarmAdmin created when no users exist
- **WHEN** `IdentitySeeder.StartAsync` is called and the users table is empty
- **THEN** one user is created with the configured email and assigned the `FarmAdmin` role

#### Scenario: Seeder skips user creation when users already exist
- **WHEN** `IdentitySeeder.StartAsync` is called and at least one user already exists
- **THEN** no new user is created

### Requirement: CustomerMappings produce correct DTO output
The `CustomerMappings` static class SHALL map `Customer`, `CustomerContact`, and `CustomerAddress` entities to their corresponding DTO records without data loss.

#### Scenario: ToSummaryDto maps all fields
- **WHEN** `ToSummaryDto` is called on a `Customer` entity with all fields populated
- **THEN** the returned `CustomerSummaryDto` contains `Id`, `Type`, `DisplayName`, `CompanyName`, `PrimaryEmail`, and `PrimaryPhone` matching the entity

#### Scenario: ToDetailDto sorts contacts primary-first
- **WHEN** `ToDetailDto` is called on a customer with multiple contacts where one has `IsPrimary = true`
- **THEN** the primary contact appears first in the returned `Contacts` collection

#### Scenario: ToDetailDto sorts addresses default-first
- **WHEN** `ToDetailDto` is called on a customer with multiple addresses where one has `IsDefault = true`
- **THEN** the default address appears first in the returned `Addresses` collection

#### Scenario: ToDetailDto handles empty child collections
- **WHEN** `ToDetailDto` is called on a customer with no contacts and no addresses
- **THEN** the returned DTO has empty (not null) `Contacts` and `Addresses` collections

### Requirement: CustomerApiClientHandler propagates user identity as X-User-Id header
The `CustomerApiClientHandler` delegating handler SHALL read `ClaimTypes.NameIdentifier` from the current `HttpContext` user and set it as the `X-User-Id` header on every outbound HTTP request to CustomerService.

#### Scenario: Header set from authenticated user claim
- **WHEN** the current `HttpContext` user has a `ClaimTypes.NameIdentifier` claim with value `"user-guid-123"`
- **THEN** the outbound HTTP request contains header `X-User-Id: user-guid-123`

#### Scenario: No header set when user is unauthenticated
- **WHEN** the current `HttpContext` user has no `ClaimTypes.NameIdentifier` claim
- **THEN** the outbound HTTP request does not contain the `X-User-Id` header
