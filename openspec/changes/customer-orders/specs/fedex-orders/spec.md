## ADDED Requirements

### Requirement: FedEx orders are one-time, manually entered by staff, and not standing orders
The system SHALL allow staff to create one-time FedEx channel orders. These orders SHALL NOT be linked to a StandingOrder and SHALL NOT auto-generate for future weeks. Staff enter them manually; no Amazon integration exists in the current release.

#### Scenario: Staff creates a FedEx order for a customer
- **WHEN** staff submits a FedEx order with CustomerId, optional ContactId, and one or more tier-size lines
- **THEN** the system creates an OrderInstance with Channel=FedEx and Status=Pending; no StandingOrder is created

#### Scenario: FedEx order cannot be converted to a standing order
- **WHEN** a FedEx order exists
- **THEN** the system SHALL NOT provide any mechanism to promote it to a standing order

### Requirement: FedEx orders use fixed oz-tier pricing that cannot be overridden
Each FedEx order line SHALL reference a tier size (1 oz – 5 lb) and its associated fixed price. Customer pricing overrides SHALL NOT apply to FedEx orders.

#### Scenario: FedEx line price matches the system-defined tier
- **WHEN** a FedEx order line is created for tier size 4 oz
- **THEN** the line price is $12.99 regardless of any customer pricing override

#### Scenario: Submitting a FedEx line with a custom price is rejected
- **WHEN** a request includes a custom price for a FedEx order line
- **THEN** the system SHALL return HTTP 400

### Requirement: FedEx packaging type is derived from total order weight
The system SHALL set the packaging type on the FedEx order automatically based on total order weight: Envelope for totals under 2 lb, Box otherwise. This is informational for staff when packing.

#### Scenario: Total weight below 2 lb sets packaging to Envelope
- **WHEN** a FedEx order's total weight is 1.5 lb
- **THEN** the PackagingType field is Envelope

#### Scenario: Total weight of 2 lb or above sets packaging to Box
- **WHEN** a FedEx order's total weight is 2 lb
- **THEN** the PackagingType field is Box

### Requirement: FedEx order instances follow the same status lifecycle as insulated instances
FedEx OrderInstance records SHALL progress through the same status sequence: Pending → Harvested → Inspected → Shipped | Cancelled, subject to the same harvest calendar and USDA inspection rules as insulated instances.

#### Scenario: FedEx instance advances through lifecycle normally
- **WHEN** a FedEx instance is in Pending status
- **THEN** it follows the same Pending → Harvested → Inspected → Shipped transitions as insulated instances

### Requirement: FedEx invoices carry the AMZ channel prefix
Invoices generated from FedEx order instances SHALL be labeled with the prefix `AMZ-` before the standard customer-key / season-year / seek-num label.

#### Scenario: FedEx invoice label includes AMZ prefix
- **WHEN** a FedEx instance is shipped and an invoice is created
- **THEN** the invoice label is `AMZ-{CustomerKey}-{SeasonYear}-{SeekNum}`

#### Scenario: FedEx SeekNum is independent of insulated SeekNum
- **WHEN** a customer has received 5 insulated invoices and 2 FedEx invoices in the same season
- **THEN** the insulated SeekNum is 5 and the FedEx SeekNum is 2; they do not share a counter
