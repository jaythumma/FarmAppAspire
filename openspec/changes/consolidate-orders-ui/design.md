## Context

The app currently has three order-related screens: `/standing-orders` (subscription list), `/orders` (order list), and `/admin/orders` (week-based generation — FarmAdmin only). Standing Orders are the subscription template for Insulated deliveries; Orders are the weekly delivery instances. FedEx orders are ad-hoc but are currently forced to auto-create a hollow StandingOrder with empty lines and `Frequency=OnRequest`, which serves no purpose and obscures the data model. Both admins and regular users find the dual-screen model confusing because a customer's subscription and their delivery history are on separate pages.

## Goals / Non-Goals

**Goals:**
- Surface Insulated subscription management (lines, skips, lifecycle) on Customer Detail
- Surface FedEx order creation and history on Customer Detail
- Merge weekly generation and order browsing into a single `/orders` fulfillment board
- Remove FedEx coupling to StandingOrder at the data model level
- Eliminate the three-screen navigation confusion without changing backend business logic

**Non-Goals:**
- Changing StandingOrder schedule generation logic (`StandingOrderScheduleService`)
- Changing pricing resolution (`PriceResolutionService`)
- Changing any Order lifecycle transitions (Pending → Harvested → Inspected → Shipped)
- Adding a customer self-service portal
- Changing the season year guard or 1-per-channel-per-customer constraint
- Splitting `OrderLine` into channel-specific types (separate concern, deferred)

## Decisions

### D1: FedEx orders use nullable `StandingOrderId`
**Decision**: `Order.StandingOrderId` becomes `Guid?`. FedEx orders have `null`; Insulated orders always have a value.

**Rationale**: FedEx is ad-hoc. The auto-create StandingOrder pattern was a workaround to satisfy a non-nullable FK — not a business requirement. Making it nullable removes the hidden side effect and is the minimal breaking change that correctly models reality.

**Alternatives considered**:
- Keep non-nullable, add `IsFedEx` flag to StandingOrder → Adds complexity, perpetuates the hollow record problem.
- Create a `FedExOrder` separate entity → Over-engineering; Orders already carry `Channel`.

### D2: `OrderFrequency.OnRequest` is removed
**Decision**: Remove the `OnRequest` value from the enum. It was only used for FedEx StandingOrders.

**Rationale**: With D1 in place, FedEx orders have no StandingOrder and therefore no Frequency. The value becomes unreachable. Keeping dead enum values is a maintenance hazard.

**Migration**: Any existing `OnRequest` StandingOrders in the DB should be evaluated — they are FedEx SOs that can be removed entirely once their associated Orders have their `StandingOrderId` nulled.

### D3: Insulated schedule lives on Customer Detail, not a global list
**Decision**: The Insulated Schedule card is added to `Customers/Detail.razor`. The `/standing-orders` global list is removed.

**Rationale**: A customer has at most one active Insulated standing order per season. "Which customer am I configuring?" is always the first question — the customer is the natural parent. The global list was only used as an entry point to find a customer's SO, which is better answered by starting at the customer.

**Alternatives considered**:
- Keep global list, add a shortcut from Customer Detail → Still two places to manage. Doesn't fix the confusion.
- Make `/standing-orders` admin-only → Reduces surface, doesn't eliminate duplication.

### D4: `/orders` becomes the unified fulfillment board
**Decision**: `/orders` gains a **This Week / All toggle**, week navigation (prev/next + date picker), and a FarmAdmin-only "Generate Insulated Orders" button. `/admin/orders` is removed.

**Rationale**: The Generate action operates on the same order list the user is already viewing — week-scoped. Keeping it in a separate admin page means admins switch screens to generate, then switch back to see results. One screen with role-gated controls is simpler.

**"This Week" default**: Operational users (farm staff, admins) care about the current week's workload first. Historical browsing is secondary. Default to current week's Monday; persist toggle preference in component state (not URL), as it's a view preference not a bookmark target.

### D5: FedEx order creation entry point moves to Customer Detail
**Decision**: The "Add Order" flow for FedEx is initiated from the FedEx Orders card on Customer Detail. The existing `/orders/create` page can remain as the creation form.

**Rationale**: FedEx orders are always for a specific customer. Starting from the customer profile means `CustomerId` is pre-populated and no channel ambiguity exists. The global `/orders` board can still link to creation with customer pre-selected.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| DB migration: `StandingOrderId` nullable may break existing EF queries that assume non-null | Audit all `db.Orders.Include(o => o.StandingOrder)` and related navigation property accesses before migration |
| Existing FedEx StandingOrders in DB become orphans after decoupling | Provide a one-time cleanup migration or admin script to null the FK on FedEx orders and delete the hollow SOs |
| Removing `/standing-orders` breaks any existing bookmarks or external links | Add a redirect from `/standing-orders` to `/orders` for a transition period |
| `OrderFrequency` enum change: EF Core stores enums as int by default — removing a value shifts nothing if stored by int, but string-stored enums break | Verify storage format in migration snapshot; add a data migration step if needed |
| Tests that assert `StandingOrderId` is non-null on FedEx orders will fail | Update FedEx order creation unit tests in Phase 1 before any UI changes |

## Migration Plan

**Phase 1 — Backend (independently deployable)**
1. Add EF Core migration: `Order.StandingOrderId` → `Guid?`
2. Update `POST /customers/{id}/orders` — remove FedEx SO auto-create branch
3. Remove `OrderFrequency.OnRequest` from enum; update `AdminEndpoints` filter if needed
4. Update unit tests; run full test suite

**Phase 2 — Customer Detail enrichment (additive, no removals)**
1. Add Insulated Schedule card to `Customers/Detail.razor` (reads existing SO endpoints)
2. Add FedEx Orders card to `Customers/Detail.razor` (reads existing Orders endpoints)
3. Deploy; verify both cards work end-to-end

**Phase 3 — Fulfillment Board consolidation (removals last)**
1. Add week toggle + navigation + Generate button to `/orders`
2. Redirect `/standing-orders` → `/orders`
3. Remove `StandingOrders/List.razor`, `StandingOrders/Detail.razor` (after confirming Detail content is fully covered by Customer Detail)
4. Remove `Admin/Orders.razor`
5. Update `NavMenu.razor`

**Rollback**: Each phase is independently reversible. Phase 1 is the only one with a DB migration — standard EF down migration restores the non-nullable FK.

## Open Questions

- Should "View all orders →" links from Customer Detail deep-link to `/orders` pre-filtered to that customer + channel? (Assume yes — pass `customerId` and `channel` as query params.)
- Should the standing order Detail page (currently `/standing-orders/{customerId}/{soId}`) be retained as a routable URL for direct links, or is the Customer Detail card sufficient? (Assume Customer Detail card is sufficient; remove the route.)
