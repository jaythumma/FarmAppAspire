## Context

Aspire's `AddPostgres(name)` provisions a Docker container backed by Npgsql. Without `.WithDataVolume()`, the container uses an anonymous ephemeral volume that Docker removes when the container stops. Calling `.WithDataVolume()` attaches a **named Docker volume** (e.g. `identity-db-data`) that survives container stops and Aspire restarts.

Both databases need the fix:

| Resource | Named volume | Stores |
|---|---|---|
| `identity-db` | `identity-db-data` | ASP.NET Core Identity users, roles, logins |
| `customer-db` | `customer-db-data` | Customers, contacts, addresses |

## Goals / Non-Goals

**Goals:**
- Data entered at runtime (customers, users) persists across `dotnet run` / `F5` restarts.
- No change to migration strategy — `MigrateAsync()` at startup already handles schema evolution idempotently.

**Non-Goals:**
- Production database hosting — this fix targets the local development Aspire container only.
- Adding a pgAdmin UI — kept out of scope to minimise change surface.
- Any backup/restore mechanism.

## Decisions

### D1 — Use `.WithDataVolume()` with default naming
**Decision:** Call `.WithDataVolume()` without an explicit volume name, letting Aspire derive the volume name from the resource name (e.g. `identity-db-data`).

**Rationale:** Aspire's default naming convention is consistent and discoverable. Explicit naming would add no value here.

**Alternative considered:** `.WithDataVolume("my-custom-name")` — rejected; custom names just add maintenance overhead.

### D2 — No `EnsureDeleted` / `EnsureCreated` guards
**Decision:** Leave the startup migration flow (`MigrateAsync`) unchanged. The existing code already handles schema idempotency.

**Rationale:** `MigrateAsync` is safe to call on both a fresh and an existing database. Adding `EnsureDeleted` guards would undermine the persistence goal.

### D3 — No changes to test projects
**Decision:** Test projects (`FarmAppAspire.Tests`, `FarmAppAspire.Tests.UI`) use Testcontainers which manage their own ephemeral containers. The `__EFMigrationsHistory` log warning in test runs is expected and harmless — Testcontainers starts a blank database intentionally.

**Rationale:** Tests must be isolated and reproducible; persistent volumes would break test isolation.

## Risks / Trade-offs

- **Schema drift:** If migrations are added between runs, `MigrateAsync` applies pending migrations against the existing volume data automatically. This is the desired behaviour.
- **Stale volume state:** A developer who wants a clean slate must run `docker volume rm identity-db-data customer-db-data` manually. This is standard practice and should be documented in the project README.
- **First-run behaviour is unchanged:** On the very first run (no volumes yet), Docker creates the volumes and EF Core runs all migrations from scratch — identical to current behaviour.

## Migration Plan

1. Edit `AppHost.cs` — add `.WithDataVolume()` to `identity-db` and `customer-db`.
2. Build and run via `dotnet run --project FarmAppAspire.AppHost`.
3. Verify: create a customer, stop the app, restart, confirm the customer still exists.
