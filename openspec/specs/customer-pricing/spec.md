## ADDED Requirements

### Requirement: Admin and Manager can set per-customer insulated pricing overrides
The system SHALL allow users with the FarmAdmin or Manager role to create, update, and delete custom pricing for a specific customer's insulated box orders via the management UI. Override pricing SHALL be scoped to a box size and MAY specify a PricePerLb, a shipping rate, and a minimum quantity threshold. FedEx channel pricing is always fixed and SHALL NOT have per-customer overrides.

#### Scenario: Manager creates a pricing override for a wholesale customer
- **WHEN** a Manager submits a pricing override for customer C with BoxSize=12lb, PricePerLb=$11.50, ShippingRate=free
- **THEN** the system stores the override and returns HTTP 201

#### Scenario: Staff cannot set FedEx pricing overrides
- **WHEN** any user attempts to create a pricing override for the FedEx channel
- **THEN** the system SHALL return HTTP 400

#### Scenario: ReadOnly user cannot access pricing override management
- **WHEN** a ReadOnly user attempts to create or update a pricing override
- **THEN** the system SHALL return HTTP 403

### Requirement: Effective price resolution uses override before default
The system SHALL resolve the effective price per pound for an insulated order line using the following priority: (1) customer-specific override for the matching box size, subject to minimum quantity; (2) the base price of $13.00 per lb. Shipping charges follow the same priority.

#### Scenario: Override applies when customer has a matching box-size entry
- **WHEN** an insulated order line is priced for customer C, BoxSize=12lb, and a matching override exists with PricePerLb=$11.50
- **THEN** the effective price is $11.50 per lb

#### Scenario: Default applies when no override exists for that box size
- **WHEN** an insulated order line is priced for a box size with no customer override
- **THEN** the effective price is $13.00 per lb

#### Scenario: Minimum quantity threshold governs override eligibility
- **WHEN** a pricing override has MinQty=50 and the order line quantity is 30
- **THEN** the default price of $13.00 per lb is used, not the override

#### Scenario: Multiple overrides coexist per customer across different box sizes
- **WHEN** customer C has override entries for both 5 lb and 12 lb boxes
- **THEN** each order line resolves independently to its matching override
