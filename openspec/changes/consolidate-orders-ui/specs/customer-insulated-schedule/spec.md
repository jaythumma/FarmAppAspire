## ADDED Requirements

### Requirement: Insulated schedule card is shown on Customer Detail
The Customer Detail page SHALL display an Insulated Schedule card when a customer has an active, paused, or stopped Insulated standing order for the current season, showing the subscription configuration and its lifecycle state.

#### Scenario: Customer has an active Insulated standing order
- **WHEN** a user navigates to Customer Detail for a customer with an Insulated standing order
- **THEN** an Insulated Schedule card is displayed showing channel, status badge, frequency, season year, box lines with quantities, and upcoming skips

#### Scenario: Customer has no Insulated standing order
- **WHEN** a user navigates to Customer Detail for a customer with no Insulated standing order
- **THEN** no Insulated Schedule card is shown for non-admin users
- **THEN** FarmAdmin users see an empty Insulated Schedule card with a "Create Schedule" action

### Requirement: Insulated schedule displays financial summary
The Insulated Schedule card SHALL display the estimated weekly total (total boxes, estimated weight, estimated amount) computed from the standing order lines and the customer's pricing overrides.

#### Scenario: Schedule has lines with pricing
- **WHEN** the Insulated Schedule card is rendered
- **THEN** the card displays total box count, estimated weight in lbs, and estimated weekly dollar amount

### Requirement: FarmAdmin can manage Insulated schedule lifecycle from Customer Detail
FarmAdmin users SHALL be able to pause, resume, and stop an Insulated standing order directly from the Insulated Schedule card on Customer Detail.

#### Scenario: Admin pauses an active schedule
- **WHEN** a FarmAdmin clicks "Pause" on an Active Insulated Schedule card
- **THEN** the standing order status is updated to Paused and the card reflects the new status

#### Scenario: Admin resumes a paused schedule
- **WHEN** a FarmAdmin clicks "Resume" on a Paused Insulated Schedule card
- **THEN** the standing order status is updated to Active and the card reflects the new status

#### Scenario: Admin stops a schedule
- **WHEN** a FarmAdmin clicks "Stop" on an Active or Paused Insulated Schedule card
- **THEN** a confirmation prompt is shown before the stop action is submitted
- **THEN** the standing order status is updated to Stopped

#### Scenario: Non-admin views lifecycle controls
- **WHEN** a non-FarmAdmin user views the Insulated Schedule card
- **THEN** Pause, Resume, and Stop actions are not visible

### Requirement: FarmAdmin can add a skip week from Customer Detail
FarmAdmin users SHALL be able to add a skip week to an Insulated standing order from the Insulated Schedule card, subject to the existing cutoff rule (before end-of-day Friday of the target week).

#### Scenario: Admin adds a valid skip week
- **WHEN** a FarmAdmin submits a skip week date that is before the cutoff
- **THEN** the skip is added and displayed in the Insulated Schedule card's skip list

#### Scenario: Admin attempts to add a skip week past the cutoff
- **WHEN** a FarmAdmin submits a skip week date that is after or on the cutoff
- **THEN** an error message is shown and the skip is not saved

### Requirement: Recent orders are shown in the Insulated Schedule card
The Insulated Schedule card SHALL display the 5 most recent Order instances generated from this standing order, showing week-of date and status.

#### Scenario: Schedule has generated orders
- **WHEN** the Insulated Schedule card is rendered and orders exist
- **THEN** up to 5 recent orders are listed with their WeekOf date and status badge

#### Scenario: User navigates to full order history
- **WHEN** a user clicks "View all orders →" in the Insulated Schedule card
- **THEN** the user is navigated to `/orders` filtered by this customer and the Insulated channel

### Requirement: FarmAdmin can edit Insulated schedule lines and frequency from Customer Detail
FarmAdmin users SHALL be able to edit the frequency and box lines of an Active or Paused Insulated standing order from the Insulated Schedule card.

#### Scenario: Admin edits frequency and lines
- **WHEN** a FarmAdmin clicks "Edit" on an Active or Paused Insulated Schedule card
- **THEN** an inline edit form is shown with frequency, monthly week (conditional), and line items
- **WHEN** the form is submitted with valid data
- **THEN** the standing order is updated and the card reflects the new configuration
