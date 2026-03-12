## ADDED Requirements

### Requirement: Invoice labels follow a structured format per channel
The system SHALL generate invoice labels in the following formats:

- **Insulated channel**: `{CustomerKey}-{SeasonYear}-{SeekNum}`
- **FedEx channel**: `AMZ-{CustomerKey}-{SeasonYear}-{SeekNum}`

Where:
- `CustomerKey` = the stored abbreviation for the customer (see `customer-key` spec)
- `SeasonYear` = the four-digit year in which the current season started (the June start year)
- `SeekNum` = a zero-padded two-digit sequential counter of invoices issued to that customer within that season year and channel

#### Scenario: First insulated invoice of a season is labeled correctly
- **WHEN** the first insulated instance for customer MAN-CHI-IL is shipped in season 2024
- **THEN** the invoice label is `MAN-CHI-IL-2024-01`

#### Scenario: Seventh invoice for a sporadic customer is labeled correctly
- **WHEN** a customer has received 6 prior insulated invoices in season 2023 and a 7th instance is shipped
- **THEN** the invoice label is `{CustomerKey}-2023-07`

#### Scenario: FedEx invoice label includes AMZ prefix and independent counter
- **WHEN** the third FedEx instance for a customer is shipped in season 2024
- **THEN** the invoice label is `AMZ-{CustomerKey}-2024-03` regardless of insulated invoice count

### Requirement: SeasonYear is determined by the June start year
The system SHALL derive the SeasonYear as the calendar year in which the current season began. A season begins on the first Monday of June and ends on the last Monday of May of the following year.

#### Scenario: Date in February resolves to prior year's season
- **WHEN** an invoice is created on February 10, 2025
- **THEN** SeasonYear = 2024 (because the current season started June 3, 2024)

#### Scenario: Date in July resolves to current year's season
- **WHEN** an invoice is created on July 15, 2024
- **THEN** SeasonYear = 2024

### Requirement: SeekNum increments sequentially per customer, per season, per channel
The system SHALL maintain a sequential invoice counter for each combination of (CustomerId, SeasonYear, Channel). The counter starts at 1 at the beginning of each season year and increments by 1 for each invoice issued. SeekNum SHALL be assigned atomically at invoice creation time to prevent gaps or duplicates under concurrent writes.

#### Scenario: SeekNum increments for each issued invoice
- **WHEN** invoices are issued to customer C in season 2024 for weeks W1, W3, and W7 (W2, W4–W6 were skipped)
- **THEN** the SeekNum values are 01, 02, 03 respectively; the label reflects orders placed, not calendar weeks

#### Scenario: SeekNum resets at season start
- **WHEN** the season year transitions from 2024 to 2025 on the first Monday of June 2025
- **THEN** the next invoice for any customer uses SeekNum=01 for SeasonYear=2025

#### Scenario: Concurrent invoice creation does not produce duplicate SeekNums
- **WHEN** two invoice creation requests for the same customer and season arrive simultaneously
- **THEN** each invoice receives a unique, sequential SeekNum (no duplicates, no gaps)

### Requirement: Sample orders do not generate invoices and do not advance the SeekNum counter
When an OrderInstance has IsSample=true, no Invoice record SHALL be created upon shipping, and the SeekNum counter for that customer SHALL NOT be incremented.

#### Scenario: Sample shipment does not create an invoice
- **WHEN** an instance with IsSample=true is shipped
- **THEN** no Invoice is created and the customer's SeekNum for that season remains unchanged
