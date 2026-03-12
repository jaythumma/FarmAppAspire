## Context

`FarmAppAspire.ApiService` is a minimal stub with one weather endpoint. There is no customer domain, no persistence, and no cross-service communication. The `evolve-auth` change will have introduced real users with Identity GUIDs. CustomerService is the first genuine business domain service: a standalone Aspire project with its own Postgres, owning the `Customer` aggregate that all future order, shipping, and invoicing features will extend.

## Goals / Non-Goals

**Goals:**
- Stand up `FarmAppAspire.CustomerService` as an independent Aspire microservice with its own Postgres database.
- Define and persist the `Customer` aggregate: type discriminator (Wholesale/Retail), contacts with roles, multiple addresses.
- Expose a clean minimal API surface for CRUD on customers, contacts, and addresses.
- Record audit fields (`CreatedBy`, `ModifiedBy` as Identity GUIDs) on every mutating operation.
- Surface a customer management UI in the Blazor frontend, gated by farm role.

**Non-Goals:**
- No orders, shipping, or invoicing in this phase — those are future phases of CustomerService.
- No event publishing (RabbitMQ) in this phase — that lands when FieldService or FinanceService is built.
- No direct database access from `webfrontend` — all customer data goes through the CustomerService API.
- CustomerService does not validate the `X-User-Id` header cryptographically — it trusts the webfrontend boundary.

## Decisions

### Decision 1 — Single-table inheritance (STI) for Wholesale vs Retail

**Chosen**: One `Customers` table with a `Type` discriminator column (`Wholesale` | `Retail`). Wholesale-only fields (`CompanyName`, `TaxId`, `PaymentTerms`) are nullable.

**Rationale**: The two types share 80% of their structure. STI means simpler queries, a single EF Core entity hierarchy, and no JOINs. The nullable columns are self-documenting and the domain is unlikely to grow beyond two customer types.

**Alternatives considered**:
- *Table-Per-Type (TPT)*: Cleaner object model but adds a JOIN on every customer query. Rejected for this scale.
- *Two separate entity types*: Doubles the API surface and adds routing complexity. Rejected.

---

### Decision 2 — Contacts and Addresses as owned child entities (separate tables, FK to Customer)

**Chosen**: `CustomerContacts` and `CustomerAddresses` as separate tables with a `CustomerId` foreign key. Both apply to Wholesale and Retail customers.

**Rationale**: A retail individual can have a primary contact plus a secondary (billing) contact. Both types can have multiple shipping and billing addresses. Separate tables are the only clean way to model one-to-many. Owned entity collections in EF Core map naturally to this.

---

### Decision 3 — Contact roles as an enum, not a free-text field

**Chosen**: `ContactRole` enum with values: `Primary`, `Billing`, `Purchasing`, `Shipping`, `Secondary`, `Other`.

**Rationale**: Roles are referenced programmatically by future order and invoice workflows (e.g., "find the billing contact for this customer"). Free text would make that lookup unreliable. Enum keeps the set closed and queryable.

---

### Decision 4 — Nested resource routes for contacts and addresses

**Chosen**: Contacts and addresses are exposed under `/customers/{id}/contacts` and `/customers/{id}/addresses` respectively.

**Rationale**: A contact or address has no meaning outside its customer. Nested routes make ownership explicit and prevent orphan records via the API. EF Core cascade delete enforces the same at the DB level.

---

### Decision 5 — `X-User-Id` header for identity propagation (trusted internal boundary)

**Chosen**: `webfrontend` injects a `CustomerApiClientHandler : DelegatingHandler` that reads `ClaimTypes.NameIdentifier` from `IHttpContextAccessor` and sets the `X-User-Id` header on every outbound request to CustomerService. CustomerService reads it as a plain string — no cryptographic validation.

**Rationale**: CustomerService has no external endpoints (`WithExternalHttpEndpoints` is not called). The network boundary is the Aspire sidecar; only `webfrontend` can reach it. Token validation would add symmetric key management complexity with no security gain inside the trusted Aspire overlay network.

---

### Decision 6 — EF Core migrations run at CustomerService startup

**Chosen**: `customerservice`'s `Program.cs` calls `dbContext.Database.MigrateAsync()` at startup, same pattern as `identity-db` in `evolve-auth`.

**Rationale**: Consistent operational pattern across all services; no separate migration-runner step needed.

## Risks / Trade-offs

- **[Risk] `X-User-Id` spoofing if CustomerService is ever exposed externally** → Mitigation: Never call `WithExternalHttpEndpoints()` on `customerservice` in AppHost. Document this constraint explicitly.
- **[Risk] STI nullable columns grow as domain evolves** → Mitigation: If a third customer type ever emerges, revisit with a migration to TPT or a type-specific extension table.
- **[Risk] No pagination on `/customers` list in phase 1** → Mitigation: Add `?page=&size=` query params from the start to avoid a breaking API change later.
- **[Risk] `customer-db` Postgres container must be healthy before CustomerService starts** → Mitigation: `WaitFor(customerDb)` in AppHost.
- **[Risk] Blazor customer UI is Interactive Server — user management pages are also Interactive Server; no state sharing issues expected but test carefully** → Mitigation: Keep each page's state local; no shared singletons across pages.

## Migration Plan

1. Create `FarmAppAspire.CustomerService` project, add to solution.
2. Add Postgres resource and project references in `AppHost.cs`.
3. Define EF Core entities and `CustomerDbContext`, generate migration.
4. Implement minimal API endpoints in `CustomerService/Program.cs`.
5. Add `CustomerApiClientHandler` and `CustomerApiClient` to `webfrontend`.
6. Build Blazor pages for customer management.
7. Smoke-test end-to-end with Aspire dashboard.

**Rollback**: Remove `customerservice` project references from AppHost and solution. No other services depend on it yet.

## Open Questions

- Should `PaymentTerms` be a free-text field or an enum (`NET30`, `NET60`, `COD`, `Prepaid`, `Other`)? Enum recommended — aligns with invoicing future state.
- Should customers have a top-level `Email` and `Phone` field in addition to contact records, or should all contact info live exclusively in `CustomerContacts`? Recommendation: keep a top-level `PrimaryEmail` and `PrimaryPhone` as a convenience denormalization for list views; contacts hold the structured detail.
