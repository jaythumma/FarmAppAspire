## Why

Every time the application starts, both Postgres containers (`identity-db` and `customer-db`) are created without persistent Docker volumes. Docker discards the container filesystem on stop, so all user-entered customer data and identity accounts are wiped on every restart. The `Failed executing DbCommand … SELECT … FROM "__EFMigrationsHistory"` error is the symptom: EF Core always finds a blank database and must re-apply migrations from scratch.

## What Changes

- Add `.WithDataVolume()` to the `identity-db` Postgres resource in `AppHost.cs` so the identity/auth database survives container restarts.
- Add `.WithDataVolume()` to the `customer-db` Postgres resource in `AppHost.cs` so customer records survive container restarts.
- No migration, model, API, or UI changes are required.

## Capabilities

### New Capabilities

*(none)*

### Modified Capabilities

*(none — no spec-level requirement changes; this is a pure infrastructure/configuration fix)*

## Impact

- **`FarmAppAspire.AppHost/AppHost.cs`** — two one-line additions, one per Postgres resource.
- Docker named volumes `identity-db-data` and `customer-db-data` are created on first run and reused on subsequent runs.
- To reset to a clean state intentionally, a developer must manually remove the Docker volumes (`docker volume rm`).
- No code changes to `FarmAppAspire.Web`, `FarmAppAspire.CustomerService`, or any test projects.
