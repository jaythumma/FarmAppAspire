## Why

FarmAppAspire has no way to track the businesses and individuals the farm sells to. A dedicated CustomerService provides the persistent, shared customer record that will serve as the foreign-key anchor for all future order, shipping, and invoicing workflows. It must be built on a solid domain model now so downstream services can reference it without structural changes.

## What Changes

- Add a new Aspire microservice project `FarmAppAspire.CustomerService` with its own Postgres database (`customer-db`).
- Define the core domain: `Customer` (Wholesale or Retail), `CustomerContact` (with roles), and `CustomerAddress` (multiple per customer, typed).
- Expose a RESTful minimal API for full CRUD on customers, with nested endpoints for contacts and addresses.
- Propagate the authenticated user's Identity GUID from `webfrontend` to `CustomerService` via the `X-User-Id` request header for audit trail (CreatedBy / ModifiedBy).
- Add a typed `HttpClient` (`CustomerApiClient`) in `FarmAppAspire.Web` following the established `WeatherApiClient` pattern.
- Add customer management pages to the Blazor frontend (customer list, detail, create/edit forms).
- Update `AppHost.cs` to provision `customer-db`, register `customerservice`, and reference it from `webfrontend`.

## Capabilities

### New Capabilities

- `customer-crud`: Create, read, update, and delete customer records (Wholesale and Retail types), scoped by organization with audit trail.
- `customer-contacts`: Manage contacts belonging to a customer, each with an assigned role (Primary, Billing, Purchasing, Shipping, Secondary, Other).
- `customer-addresses`: Manage multiple addresses per customer, each labelled, typed (Billing / Shipping / Both), with a default flag.
- `customer-ui`: Blazor pages for listing, viewing, creating, and editing customers, contacts, and addresses; access controlled by farm role.
- `user-id-propagation`: The webfrontend forwards the authenticated user's Identity GUID to CustomerService via the `X-User-Id` header on every outbound request.

### Modified Capabilities

<!-- No existing specs have requirement changes for this change. -->

## Impact

- **New project**: `FarmAppAspire.CustomerService` — minimal API, EF Core, Npgsql, `AddServiceDefaults()`.
- **New project**: added to `FarmAppAspire.slnx` solution.
- `FarmAppAspire.AppHost/AppHost.cs` — add `AddPostgres("customer-db")`, `AddProject<CustomerService>("customerservice")`, update `webfrontend` reference.
- `FarmAppAspire.Web` — add `CustomerApiClient`, DelegatingHandler for `X-User-Id` header, customer Blazor pages, role-gated nav links.
- No changes to `FarmAppAspire.ApiService` or `FarmAppAspire.ServiceDefaults`.
- New NuGet packages in `CustomerService`: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`.
- **Depends on**: `evolve-auth` must be complete — requires real `UserId` in the cookie claims.
