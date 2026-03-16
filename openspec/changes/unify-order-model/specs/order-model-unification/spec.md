## ADDED Requirements

### Requirement: Order is the primary delivery entity
The system SHALL use `Order` as the canonical name for a concrete delivery for a specific week. The entity previously named `OrderInstance` SHALL be renamed to `Order` and `OrderInstanceLine` SHALL be renamed to `OrderLine` at all layers: EF entity, database table, DTOs, enums, API endpoints, web client, and UI.

#### Scenario: Order entity name is consistent across all layers
- **WHEN** a developer navigates any layer of the codebase (model, endpoint, client, or UI)
- **THEN** the term `Order` (not `OrderInstance`) is used to refer to a concrete weekly delivery

#### Scenario: Database tables are renamed
- **WHEN** the migration runs
- **THEN** the `OrderInstances` table is renamed to `Orders` and `OrderInstanceLines` is renamed to `OrderLines`

---

### Requirement: Every Order MUST be linked to a StandingOrder
The system SHALL enforce that `Order.StandingOrderId` is non-nullable. No `Order` record SHALL exist in the database without a valid `StandingOrderId` foreign key reference.

#### Scenario: Order cannot be created without a StandingOrder
- **WHEN** the order creation endpoint is called for a customer
- **THEN** the system finds or auto-creates a `StandingOrder` before persisting the `Order`
- **THEN** the `Order.StandingOrderId` is populated with a valid `StandingOrder.Id`

#### Scenario: Existing orphaned orders are backfilled during migration
- **WHEN** the migration runs and `OrderInstances` records exist with `StandingOrderId IS NULL`
- **THEN** the migration creates `StandingOrders` for those records grouped by `(CustomerId, Channel)`
- **THEN** the migration sets `StandingOrderId` on those records before applying the NOT NULL constraint

#### Scenario: Deleting a StandingOrder that has Orders is rejected
- **WHEN** a `StandingOrder` that has associated `Orders` is targeted for deletion
- **THEN** the database rejects the deletion with a referential integrity error (`Restrict` behavior)

---

### Requirement: StandingOrder has an explicit Channel
The system SHALL store a `Channel` (`OrderChannel`: `Insulated` or `FedEx`) on `StandingOrder`. A customer SHALL have at most one `StandingOrder` per `Channel` (enforced by a unique index on `(CustomerId, Channel)`).

#### Scenario: StandingOrder channel is persisted
- **WHEN** a `StandingOrder` is created
- **THEN** its `Channel` field is set to either `Insulated` or `FedEx` and persisted

#### Scenario: Duplicate channel standing order is rejected
- **WHEN** a second `StandingOrder` is created for a customer with the same `Channel`
- **THEN** the database rejects the insert with a unique constraint violation

#### Scenario: Existing StandingOrders are backfilled with Channel = Insulated
- **WHEN** the migration runs
- **THEN** all existing `StandingOrders` without a `Channel` value are set to `Channel = 'Insulated'`

---

### Requirement: FedEx StandingOrders use OnRequest frequency with empty lines
The system SHALL auto-create FedEx `StandingOrders` with `Frequency = OnRequest` and an empty `Lines` collection. The `Lines` navigation on a FedEx `StandingOrder` SHALL always be empty; FedEx order trend analysis is performed through the linked `Orders`, not through standing order lines.

#### Scenario: FedEx StandingOrder is created with OnRequest frequency
- **WHEN** a FedEx `Order` is placed for a customer who has no FedEx `StandingOrder`
- **THEN** the system creates a `StandingOrder` with `Channel = FedEx`, `Frequency = OnRequest`, and `Lines = []`

#### Scenario: FedEx StandingOrder totals are zero
- **WHEN** a FedEx `StandingOrder` detail is retrieved
- **THEN** `TotalBoxes`, `TotalWeightLbs`, and `TotalAmount` are all `0`

---

### Requirement: Admin bulk generate operates only on Insulated standing orders
The system SHALL filter standing orders by `Channel = Insulated` before bulk-generating `Orders` for a week. FedEx standing orders SHALL never be included in admin bulk generation regardless of their `Frequency` or `Status`.

#### Scenario: Admin generate skips FedEx standing orders
- **WHEN** `POST /admin/generate-orders` is called for a given week
- **THEN** no `Order` is generated for any `StandingOrder` with `Channel = FedEx`

#### Scenario: Admin generate processes Insulated OnRequest standing orders
- **WHEN** `POST /admin/generate-orders` is called for a given week
- **THEN** `StandingOrders` with `Channel = Insulated` and `Frequency = OnRequest` are still processed (generated once, then auto-paused)

---

### Requirement: Standing-order auto-create is removed from invoice creation
The system SHALL NOT create a `StandingOrder` inside the invoice creation path. By the time an `Order` is shipped and invoiced, `StandingOrderId` SHALL already be set.

#### Scenario: Invoice creation does not create StandingOrders
- **WHEN** `POST /customers/{id}/orders/{orderId}/ship` is called and an invoice is created
- **THEN** no `StandingOrder` is inserted as a side effect of the invoice creation

---

### Requirement: API routes use Order terminology
All API routes that previously used `order-instances` or `instances` terminology SHALL be renamed to use `orders`.

#### Scenario: Order list route
- **WHEN** a client calls `GET /orders` or `GET /customers/{id}/orders`
- **THEN** the server responds with HTTP 200 and the order list (was: `/order-instances`)

#### Scenario: Admin generate route
- **WHEN** a client calls `POST /admin/generate-orders`
- **THEN** the server generates orders for the given week (was: `/admin/generate-instances`)

#### Scenario: Admin week browse route
- **WHEN** a client calls `GET /admin/orders?weekOf=...`
- **THEN** the server returns the week's orders (was: `/admin/instances`)
