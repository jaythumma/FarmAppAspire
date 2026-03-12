## ADDED Requirements

### Requirement: A standing order is auto-created when a customer is first invoiced
The system SHALL automatically create a StandingOrder for a customer the first time an invoice is issued to them. The auto-created standing order SHALL default to Weekly frequency with IsSample set to false. Staff MAY modify frequency and other fields after creation.

#### Scenario: Auto-creation on first invoice
- **WHEN** an invoice is generated for a customer who has no existing standing order
- **THEN** the system creates a StandingOrder with Frequency=Weekly, IsSample=false, Status=Active, StartWeek=the Monday of that invoice's week

#### Scenario: No duplicate standing order created
- **WHEN** an invoice is generated for a customer who already has a standing order
- **THEN** the system SHALL NOT create a second standing order

### Requirement: Standing orders support multiple lines with aggregated totals
A standing order SHALL contain one or more order lines, each specifying a box size and quantity. The system SHALL compute and return aggregated totals: TotalBoxes (sum of all line quantities), TotalWeightLbs (sum of BoxSizeLbs × Qty per line), and TotalAmount (sum of EffectivePrice × WeightLbs per line).

#### Scenario: Standing order with multiple lines returns correct aggregated totals
- **WHEN** a standing order has lines `[5lb × 3, 12lb × 10]`
- **THEN** TotalBoxes = 13, TotalWeightLbs = (3×5) + (10×12) = 135 lb, TotalAmount reflects effective pricing per line

#### Scenario: Empty order is rejected
- **WHEN** a standing order is submitted with no lines
- **THEN** the system SHALL return HTTP 400

### Requirement: Standing orders carry a sample flag that suppresses invoicing
When `IsSample` is true, the system SHALL fulfill the order instance normally but SHALL NOT generate an invoice. Staff MAY set `IsSample=false` on the standing order at any time, at which point subsequent instances will be invoiced. Transitioning IsSample from true to false on an existing standing order is the mechanism for converting a sample customer to a paying customer.

#### Scenario: Sample instance is fulfilled but not invoiced
- **WHEN** a standing order has IsSample=true and an instance is shipped
- **THEN** the system marks the instance as Shipped but does not create an Invoice record

#### Scenario: Turning off IsSample starts invoicing from the next instance
- **WHEN** staff sets IsSample=false on an existing standing order
- **THEN** the next shipped instance generates an invoice; previously shipped sample instances remain uninvoiced

### Requirement: Standing orders are scoped to the season year
A standing order's season year is the calendar year in which the current season began (the June start year). The system SHALL use the season year when labeling invoices and when tracking the sequential invoice counter per customer. The system SHALL support a staff-triggered season restart that resets the SeekNum counter for the new season year.

#### Scenario: Season year is derived from June start date
- **WHEN** today's date is within the range first Monday of June 2024 → last Monday of May 2025
- **THEN** the season year is 2024

#### Scenario: Season restart resets invoice counter
- **WHEN** staff triggers a season restart for season year 2025
- **THEN** the SeekNum counter for all customers resets to 1 for SeasonYear=2025

### Requirement: At most one active standing order exists per customer
The system SHALL enforce that a customer has at most one standing order in Active or Paused status at a time. Future support for multiple concurrent standing orders is an explicit non-goal for this release.

#### Scenario: Attempting to create a second active standing order is rejected
- **WHEN** a customer already has an Active standing order and staff attempts to create another
- **THEN** the system SHALL return HTTP 409
