## Context

The farm's order workflow is entirely manual today. The `FarmAppAspire.CustomerService` microservice already owns Customer, Contact, and Address entities backed by a PostgreSQL database with EF Core migrations. The order domain is additive — it extends the same microservice and database rather than creating a new Aspire service, keeping deployment and cross-service complexity low at this stage.

Key constraints from exploration:
- Only one product (Curry Leaf) with two fulfillment channels (Insulated direct, FedEx/Amazon).
- Season year runs first Monday of June → last Monday of May.
- Standing orders auto-generate weekly instances; staff drive lifecycle transitions.
- USDA inspection happens only on Fridays and Mondays — this is a hard calendar constraint.
- Invoice labeling requires a customer-scoped sequential counter that resets each season.

## Goals / Non-Goals

**Goals:**
- Model all order entities (StandingOrder, OrderInstance, Invoice) within `CustomerDbContext`.
- Implement the full standing-order lifecycle: create → generate instances → harvest → inspect → ship → invoice.
- Implement FedEx one-time order entry and invoicing.
- Expose all operations as minimal API endpoints in `CustomerService/Program.cs`.
- Provide Blazor management UI pages for order setup, scheduling, and fulfillment tracking.

**Non-Goals:**
- Order payment processing (future spec).
- Delivery/shipment logistics tracking (trucks, 18-wheelers) — future spec.
- Amazon FedEx channel automation/integration — manual entry only for now.
- Multiple simultaneous standing orders per customer — future feature.
- Authorized-contact restrictions — any contact may order for now.
- Customer self-serve portal.
- Farm section / bed / inspection origin tracking.

## Decisions

### D1 — Extend CustomerDbContext, not a new DbContext
**Decision:** Add all order entities to the existing `CustomerDbContext` in `CustomerService`.

**Rationale:** Keeps foreign-key relationships between Customer/Contact/Address and orders in a single transaction boundary. Avoids distributed-transaction complexity at this stage. The CustomerService microservice is the correct domain owner.

**Alternative:** New `OrderDbContext` — rejected; no benefit at single-service scale, adds migration management overhead.

---

### D2 — Standing order line as a child entity, not value object
**Decision:** `StandingOrderLine` is a persisted entity with its own `Id`, linked to `StandingOrder`.

**Rationale:** Multiple lines per order (e.g. `5lb × 3` + `12lb × 10`) need to be independently editable and queryable. Aggregated totals (TotalBoxes, TotalWeightLbs, TotalAmount) are computed at read time, not stored.

**Alternative:** JSON column for lines — rejected; poor queryability, harder to migrate.

---

### D3 — Frequency as an enum + MonthlyWeek ordinal
**Decision:** `Frequency` enum: `Weekly | BiWeekly | Monthly | OnRequest | Stopped`. For `Monthly`, a separate `MonthlyWeek` ordinal (1–4) stores which Monday of the month.

**Rationale:** Simple to store, reason about, and display in UI. Avoids cron-string complexity.

**Alternative:** Cron expression string — rejected; overkill for 5 fixed patterns, unreadable in UI.

---

### D4 — SkipWeeks as a separate join table
**Decision:** `StandingOrderSkip` entity stores `(StandingOrderId, WeekOf)` pairs.

**Rationale:** Queryable, auditable, can be deleted cleanly when skips are rescinded. The Friday cutoff check queries this table filtered to `WeekOf >= today`.

---

### D5 — Season year and SeekNum stored on Invoice, not computed
**Decision:** `Invoice.SeasonYear` (int) and `Invoice.SeekNum` (int) are stored columns. The label `{CustomerKey}-{SeasonYear}-{SeekNum}` is derived at read time or stored as a computed column.

**Rationale:** SeekNum must be assigned atomically when invoice is created (race condition risk if computed on the fly). Storing it avoids recomputation and label drift if customer key changes.

**Alternative:** Compute from count — rejected; fragile under deletions or gaps.

---

### D6 — BoxSize and FedExTier as static seed data
**Decision:** `InsulatedBoxConfig` and `FedExTierConfig` rows are seeded via EF Core `HasData`. These are not user-editable; only a migration can change them.

**Rationale:** Prices and sizes are business constants referenced throughout the system. Seed data survives schema changes cleanly. FedEx tier pricing is explicitly non-negotiable — no admin UI for it.

---

### D7 — OrderInstance.Status transitions enforced in service layer
**Decision:** Status transitions (`Pending → Harvested → Inspected → Shipped | Cancelled`) are validated in the endpoint handler. Invalid transitions return HTTP 422.

**Rationale:** Business rules (e.g. can only ship after inspection, can only cancel a Pending instance before Friday cutoff) belong in the domain layer, not the DB.

---

### D8 — Standing order auto-generation as a scheduled background service
**Decision:** A hosted `IHostedService` (`StandingOrderGenerationService`) runs on a configurable schedule (weekly, Monday-based) to generate `OrderInstance` rows from active `StandingOrder` records.

**Rationale:** Aspire's hosting model supports background services naturally. The generation logic is deterministic and idempotent (skip if instance already exists for that WeekOf).

**Alternative:** External cron job / Azure Function — rejected; adds infrastructure dependency. Manual-only trigger — rejected; defeats the automation requirement.

---

### D9 — Customer key generation as a deterministic utility
**Decision:** `CustomerKey` is computed from `DisplayName` abbreviation + `City`/`State` abbreviation using a deterministic algorithm (take consonants / first letters, truncate to fixed length). Stored on `Customer` record when first computed; recalculated only on explicit staff action.

**Rationale:** The key appears on every invoice — it must be stable. Storing it avoids runtime computation drift. Details are owned by the `customer-key` spec.

---

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| SeekNum assignment race condition (two invoices created simultaneously for the same customer/season) | Use a DB-level sequence or pessimistic lock on Invoice insert |
| Standing order generation runs after USDA inspection cutoff | Generation service runs early Monday; idempotency check prevents duplicates |
| BiWeekly rhythm disrupted by skip — resumption ambiguity | Spec 4 defines precise rule: resume biweekly from the week the customer resumes, not the skipped week |
| Customer key collisions between similar display names | Store generated key; flag collision at generation time; staff manually resolves |
| Large number of OrderInstance rows over seasons | Index on `(CustomerId, WeekOf, Status)`; archive old seasons as needed |

## Migration Plan

1. Create EF Core migration adding order tables to `CustomerDbContext`.
2. Seed static `InsulatedBoxConfig` and `FedExTierConfig` rows.
3. Deploy updated `CustomerService`.
4. No rollback concerns — fully additive, no existing columns altered.

## Open Questions

- **SeekNum atomicity**: Decide between `SELECT MAX(SeekNum) + 1 FOR UPDATE` vs a PostgreSQL sequence per `(CustomerId, SeasonYear, Channel)`. Sequence approach is cleaner but requires dynamic sequence creation.
- **Invoice generation timing**: Invoice is created when instance reaches `Shipped`. Should `ShipDate` (Fri vs Mon) be explicitly set by staff, or inferred from the day the transition occurs?
- **Standing order line effective price**: Is it resolved at generation time (locked in) or at shipping time (could change if pricing is updated mid-week)?
