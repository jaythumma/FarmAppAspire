## Context

The `CustomerService` currently has two overlapping concepts: `StandingOrder` (a recurring template) and `OrderInstance` (a concrete delivery for a week). The name "instance" implies it is secondary to something primary, but in the operational domain the `OrderInstance` **is** the primary artifact — it is what gets harvested, inspected, shipped, and invoiced. `StandingOrder` is the customer's relationship contract with the farm.

Additional problems in the current state:
- `OrderInstance.StandingOrderId` is nullable with `OnDelete(SetNull)`, which allows free-floating orders with no standing order link.
- FedEx orders are created via a separate `FedExOrderEndpoints` that never sets `StandingOrderId`, breaking the customer-relationship pattern.
- A `StandingOrder` has no `Channel` field — it is implicitly Insulated-only despite FedEx following the same contract pattern.
- Auto-creation of a `StandingOrder` is buried inside `CreateInvoiceAsync`, making it invisible and triggering at the wrong time (ship, not order placement).
- The admin bulk-generate endpoint would incorrectly process FedEx standing orders if they existed, because there is no channel filter.
- The web frontend's `CreateEdit.razor` branches on channel to call either `CreateStandingOrderAsync` or `CreateFedExOrderAsync`, encoding the inconsistency into the UI.

## Goals / Non-Goals

**Goals:**
- Rename `OrderInstance` → `Order` and `OrderInstanceLine` → `OrderLine` at every layer (entity, table, DTO, enum, endpoint, client, UI, tests).
- Make `Order.StandingOrderId` non-nullable — every `Order` MUST be linked to a `StandingOrder`.
- Add `Channel` (`OrderChannel`) to `StandingOrder`, with a unique index on `(CustomerId, Channel)`.
- Unify order creation into `POST /customers/{id}/orders`, which auto-creates a `StandingOrder` when none exists for that `(CustomerId, Channel)` pair.
- Admin bulk generate (`POST /admin/generate-orders`) filters exclusively on `Channel = Insulated` standing orders.
- Remove standing-order auto-create logic from `CreateInvoiceAsync`.
- Provide a safe multi-step EF migration with data backfill.

**Non-Goals:**
- Changing the business rules of order lifecycle (Pending → Harvested → Inspected → Shipped | Cancelled).
- Changing skip logic, season restart logic, or BiWeekly cadence calculation.
- Adding FedEx template lines to `StandingOrderLine` — FedEx standing orders have empty lines; trend tracking is via linked `Orders`.
- UI redesign beyond unifying the creation form and updating route/label references.
- Retry or circuit-breaker UI improvements (covered by `AddStandardResilienceHandler`).
- Changing `Invoice` structure or `InvoiceLabelService`.

## Decisions

### D1 — Full rename at all layers, not a facade

**Choice:** Rename the C# entity, EF table, DTOs, endpoints, client records, and UI copy from `OrderInstance` → `Order`.

**Rationale:** A facade rename (keep `OrderInstance` in C# but expose it as "Order" via routes and UI) would leave the internal codebase inconsistent and confuse the next developer. The cost of a full rename is a one-time migration; the benefit is permanent clarity.

**Alternative considered:** Keep `OrderInstance` internally, rename only routes/UI labels. Rejected — perpetuates the confusion in tests, services, and IDE navigation.

### D2 — `StandingOrderId` becomes required (NOT NULL)

**Choice:** `Order.StandingOrderId` changes from `Guid?` to `Guid` (non-nullable); EF relationship changes from `OnDelete(SetNull)` to `OnDelete(Restrict)`.

**Rationale:** The invariant "every order belongs to a standing order" is the core model fix. Making it nullable was a workaround for FedEx orders that didn't create standing orders. With the unified creation flow (D4), no code path produces a standing-order-less order.

**Migration risk:** Existing `Orders` (previously `OrderInstances`) where `StandingOrderId IS NULL` must be backfilled before the NOT NULL constraint is applied. The migration backfills by auto-creating `StandingOrders` for any orphaned records grouped by `(CustomerId, Channel)`.

**Down migration note:** The `Down()` migration cannot safely drop the auto-created standing orders because new orders may have been added to them. The `Down()` will restore nullability and `SetNull` behavior but leave the backfilled standing orders in place.

### D3 — Add `Channel` to `StandingOrder` with unique index on `(CustomerId, Channel)`

**Choice:** A new `Channel` property (`OrderChannel` enum, stored as string) on `StandingOrder`, with a unique index enforcing at most one standing order per customer per channel.

**Rationale:** This makes the customer-relationship-per-channel explicit and enables the lookup "find or create the Insulated standing order for customer X" or "find or create the FedEx standing order for customer X" without ambiguity.

**Alternative considered:** Allow multiple standing orders per customer per channel (e.g., multiple insulated schedules). Rejected for now — the current domain has one relationship per channel. Multiple can be revisited as a future capability.

**FedEx StandingOrder lines:** FedEx `StandingOrderLine` records are not meaningful (FedEx orders are always specified at placement time), so auto-created FedEx standing orders have empty `Lines`. This is correct — the `StandingOrder.Lines` navigation will return an empty list and totals will be zero, which is accurate.

### D4 — Unified creation endpoint replaces two separate creation paths

**Choice:** `POST /customers/{id}/orders` with `{ channel, weekOf, lines, contactId, isSample }` replaces `POST /customers/{id}/standing-orders` (insulated, UI only) and `POST /customers/{id}/fedex-orders`.

**Rationale:** The two separate paths encode the channel asymmetry into the API contract. A single endpoint with a `channel` discriminator is consistent, easier to document, and maps cleanly to the UI's "Add Order" form.

**Standing order lookup/create logic:**
```
find StandingOrder WHERE CustomerId = id AND Channel = channel AND Status != Stopped
if not found:
  create StandingOrder:
    - Insulated: Frequency = Weekly, Lines = from request lines, StartWeek = weekOf
    - FedEx:     Frequency = OnRequest, Lines = [], StartWeek = weekOf
create Order linked to that StandingOrder
```

**Idempotency:** Duplicate prevention (same `StandingOrderId` + `WeekOf` already exists) is checked before creating the `Order`, matching the existing admin generate behavior.

**Alternative considered:** Keep `POST /standing-orders` as an explicit pre-step before placing an order. Rejected — it forces the caller to know whether a standing order exists. The unified endpoint is better for the UI and for FedEx which should never require a separate standing order step.

### D5 — Admin generate filters on `Channel = Insulated` only

**Choice:** `POST /admin/generate-orders` adds `.Where(s => s.Channel == OrderChannel.Insulated)` before loading standing orders for bulk generation.

**Rationale:** FedEx orders are on-demand by definition. Auto-generating them would create unexpected orders. The `Channel` field makes this filter explicit and safe.

**`OnRequest` insulated orders:** `StandingOrderScheduleService.ShouldGenerateForWeek` already returns `true` for `OnRequest` and the caller auto-pauses afterward. This behavior is preserved — insulated customers with `OnRequest` frequency still participate in admin generation.

### D6 — Remove auto-create from `CreateInvoiceAsync`

**Choice:** Delete the standing-order auto-create block in `OrderInstanceEndpoints.CreateInvoiceAsync`.

**Rationale:** Invoice creation has no business creating customer contracts. The standing order must exist before the order is created (enforced by D2). By the time `CreateInvoiceAsync` runs, `StandingOrderId` is guaranteed non-null.

**Alternative considered:** Leave it as a safety net. Rejected — it becomes unreachable dead code once D2 is in place, and dead code that creates records is dangerous.

### D7 — `FedExOrderEndpoints` is deleted, merged into `OrderEndpoints`

**Choice:** Delete `FedExOrderEndpoints.cs` and `MapFedExOrderEndpoints()`; merge GET and POST for FedEx into `OrderEndpoints.cs` with channel filtering.

**Rationale:** `FedExOrderEndpoints` is now redundant. All order operations are channel-aware, so a separate endpoint class for one channel is unnecessary.

## Risks / Trade-offs

- **Risk: Data migration for orphaned FedEx orders** — existing `OrderInstances` with `StandingOrderId IS NULL` must be backfilled. If the backfill runs after the NOT NULL constraint is applied, the migration will fail. → **Mitigation:** Migration `Up()` uses explicit steps: (1) add nullable `Channel` to `StandingOrders`, (2) backfill existing standing orders to `Channel = 'Insulated'`, (3) create standing orders for orphaned records, (4) backfill FKs, (5) alter column to NOT NULL. These steps must remain in this exact order.

- **Risk: Unique index `(CustomerId, Channel)` on `StandingOrders` fails if duplicates exist** — a customer may already have two standing orders for the same channel. → **Mitigation:** Query for duplicates before applying the index in the migration; if found, merge them (assign all instances to the oldest one) or log and fail loudly. Add a pre-migration check script.

- **Risk: `OrderFrequency.OnRequest` FedEx standing orders being picked up by future generate runs** — if the `Channel` filter is omitted in future code. → **Mitigation:** The `Channel = Insulated` filter in `AdminEndpoints` is the authoritative gate; add an XML doc comment making this explicit. Unit test asserts FedEx SOs are excluded from generate.

- **Trade-off: `StandingOrder.Lines` is Insulated-only** — FedEx standing orders have structurally empty `Lines`. The totals for a FedEx standing order (boxes, weight, amount) are always 0. → Accepted. FedEx trend analysis is done through linked `Orders`, not through the standing order's own lines.

- **Trade-off: `POST /customers/{id}/standing-orders` is removed from the public API** — any external client calling this endpoint directly will break. → Accepted. This is an internal API; no external consumers are documented.

## Migration Plan

```
Step 1  Add nullable Channel column to StandingOrders (DEFAULT NULL)
Step 2  UPDATE StandingOrders SET Channel = 'Insulated' WHERE Channel IS NULL
Step 3  For each OrderInstance WHERE StandingOrderId IS NULL:
          GROUP BY (CustomerId, Channel)
          INSERT StandingOrder (Channel, Frequency=OnRequest, StartWeek=MIN(WeekOf), ...)
          UPDATE OrderInstance SET StandingOrderId = <new SO id>
Step 4  ALTER StandingOrders.Channel NOT NULL
Step 5  ALTER OrderInstances.StandingOrderId NOT NULL
Step 6  DROP FOREIGN KEY with SetNull behavior; ADD FOREIGN KEY with Restrict
Step 7  Add unique index IX_StandingOrders_CustomerId_Channel
Step 8  Rename table OrderInstances → Orders
Step 9  Rename table OrderInstanceLines → OrderLines
Step 10 Update EF model snapshot
```

Rollback: Restore nullability on both columns and drop the unique index. The auto-created standing orders remain but are harmless.

Normal redeploy — no infrastructure changes, no AppHost changes, no Redis or config changes.

## Open Questions

*(none — all decisions were made during exploration)*
