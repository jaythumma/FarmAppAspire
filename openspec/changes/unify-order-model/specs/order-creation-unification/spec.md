## ADDED Requirements

### Requirement: Unified order creation endpoint handles both channels
The system SHALL provide a single endpoint `POST /customers/{id}/orders` that accepts both Insulated and FedEx orders via a `Channel` discriminator. The separate `POST /customers/{id}/fedex-orders` endpoint SHALL be removed. The `POST /customers/{id}/standing-orders` endpoint SHALL be removed from the creation path (standing orders are auto-created by the order endpoint).

#### Scenario: Place an Insulated order
- **WHEN** `POST /customers/{id}/orders` is called with `Channel = Insulated` and valid insulated lines
- **THEN** the server creates an `Order` with `Channel = Insulated` and returns HTTP 201 with the `OrderDto`

#### Scenario: Place a FedEx order
- **WHEN** `POST /customers/{id}/orders` is called with `Channel = FedEx` and valid FedEx lines
- **THEN** the server creates an `Order` with `Channel = FedEx` and returns HTTP 201 with the `OrderDto`

#### Scenario: FedEx order endpoint no longer exists
- **WHEN** a client calls `POST /customers/{id}/fedex-orders`
- **THEN** the server responds with HTTP 404

---

### Requirement: Order creation auto-creates a StandingOrder when none exists
The system SHALL find an existing active `StandingOrder` for the `(CustomerId, Channel)` pair when a new `Order` is placed. If no such `StandingOrder` exists, the system SHALL automatically create one before creating the `Order`. This auto-create SHALL happen at order placement time, not at invoice time.

#### Scenario: Auto-create Insulated StandingOrder on first order
- **WHEN** `POST /customers/{id}/orders` is called with `Channel = Insulated` and no `StandingOrder` exists for that customer and channel
- **THEN** the system creates a `StandingOrder` with `Channel = Insulated`, `Frequency = Weekly`, `StartWeek = weekOf`, and `Lines` copied from the request
- **THEN** the system creates an `Order` linked to the new `StandingOrder`

#### Scenario: Auto-create FedEx StandingOrder on first order
- **WHEN** `POST /customers/{id}/orders` is called with `Channel = FedEx` and no `StandingOrder` exists for that customer and channel
- **THEN** the system creates a `StandingOrder` with `Channel = FedEx`, `Frequency = OnRequest`, `StartWeek = weekOf`, and empty `Lines`
- **THEN** the system creates an `Order` linked to the new `StandingOrder`

#### Scenario: Reuse existing StandingOrder on subsequent order
- **WHEN** `POST /customers/{id}/orders` is called and an active `StandingOrder` already exists for that `(CustomerId, Channel)` pair
- **THEN** the system does NOT create a new `StandingOrder`
- **THEN** the new `Order` is linked to the existing `StandingOrder`

#### Scenario: Duplicate order for same week is rejected
- **WHEN** `POST /customers/{id}/orders` is called with a `weekOf` for which an `Order` already exists under the same `StandingOrder`
- **THEN** the server responds with HTTP 409 (Conflict)

---

### Requirement: Order creation validates customer and contact
The system SHALL validate that the customer exists and that the `contactId`, if provided, belongs to that customer before creating an `Order` or auto-creating a `StandingOrder`.

#### Scenario: Unknown customer returns 404
- **WHEN** `POST /customers/{id}/orders` is called with a `customerId` that does not exist
- **THEN** the server responds with HTTP 404

#### Scenario: Contact not belonging to customer returns 400
- **WHEN** `POST /customers/{id}/orders` is called with a `contactId` that does not belong to the specified customer
- **THEN** the server responds with HTTP 400

#### Scenario: At least one order line is required
- **WHEN** `POST /customers/{id}/orders` is called with an empty `lines` array
- **THEN** the server responds with HTTP 400

---

### Requirement: Unified order creation is reflected in the web frontend
The Blazor `CreateEdit.razor` for Orders SHALL use a single creation path regardless of channel, calling the unified `POST /customers/{id}/orders` endpoint. The UI SHALL NOT branch between `CreateStandingOrderAsync` and `CreateFedExOrderAsync`.

#### Scenario: New Insulated order created through UI
- **WHEN** a user fills out the "Add Order" form with `Channel = Insulated` and submits
- **THEN** the UI calls the unified `CreateOrderAsync` method which posts to `/customers/{id}/orders`
- **THEN** the user is redirected to the order detail page

#### Scenario: New FedEx order created through UI
- **WHEN** a user fills out the "Add Order" form with `Channel = FedEx` and submits
- **THEN** the UI calls the unified `CreateOrderAsync` method which posts to `/customers/{id}/orders`
- **THEN** the user is redirected to the order detail page
