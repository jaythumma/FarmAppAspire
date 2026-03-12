## ADDED Requirements

### Requirement: Standing orders support five frequency options
The system SHALL support the following frequency values on a standing order:

| Frequency | Behavior |
|---|---|
| Weekly | Instance generated every Monday of the season |
| BiWeekly | Instance generated every other Monday, alternating from StartWeek |
| Monthly | Instance generated on the nth Monday of each month (1st, 2nd, 3rd, or 4th) |
| OnRequest | Instance generated and shipped for the current week; auto-pauses for all subsequent weeks |
| Stopped | No further instances are generated; standing order is permanently ended |

#### Scenario: Weekly standing order generates an instance every Monday
- **WHEN** a standing order has Frequency=Weekly and Status=Active
- **THEN** the system generates one OrderInstance for every Monday within the active season

#### Scenario: Monthly standing order on 2nd Monday generates correctly
- **WHEN** a standing order has Frequency=Monthly, MonthlyWeek=2
- **THEN** instances are generated only on the second Monday of each month

#### Scenario: Stopped standing order generates no new instances
- **WHEN** a standing order has Frequency=Stopped
- **THEN** the system SHALL NOT generate any OrderInstance records for future weeks

### Requirement: Friday cutoff deadline governs skip requests
The system SHALL enforce a cutoff of end-of-day Friday for skip requests that apply to that Monday's shipment. Any skip request received after the Friday cutoff is ignored — the instance ships and is invoiced as normal.

#### Scenario: Valid skip before Friday cutoff cancels the instance
- **WHEN** a skip request is submitted for a customer's standing order before EOD Friday of the harvest week
- **THEN** the system cancels the OrderInstance for that Monday (Status=Cancelled) and adds the Monday date to StandingOrderSkip

#### Scenario: Skip request after Friday cutoff is rejected
- **WHEN** a skip request is submitted on or after Saturday of the harvest week
- **THEN** the system SHALL return HTTP 422 with a message indicating the cutoff has passed; the instance is NOT cancelled

#### Scenario: Cancelled instance is never invoiced
- **WHEN** an OrderInstance has Status=Cancelled
- **THEN** no Invoice is created for that instance

### Requirement: BiWeekly rhythm resumes from the week the order restarts after a skip
When a BiWeekly standing order has an instance skipped (cancelled before cutoff), the alternating rhythm SHALL restart biweekly from the next active Monday after the skipped week, not from the originally scheduled slot.

#### Scenario: BiWeekly rhythm restarts after a skip
- **WHEN** a BiWeekly standing order would have shipped on Monday W, that week is skipped, and the order resumes on Monday W+1
- **THEN** the next instance is generated on Monday W+3 (biweekly from the resume point), not Monday W+2

### Requirement: OnRequest standing orders generate one instance at a time then auto-pause
A standing order with Frequency=OnRequest SHALL generate an OrderInstance for the current week when staff releases it from paused status. After that instance is generated, the standing order SHALL automatically return to Paused status for all subsequent weeks. Staff MUST manually release it again for each future instance.

#### Scenario: Releasing an OnRequest order generates one instance
- **WHEN** staff releases a standing order with Frequency=OnRequest from Paused
- **THEN** the system generates one OrderInstance for the current week and immediately sets the standing order back to Paused

#### Scenario: OnRequest instance is fulfilled normally
- **WHEN** an OnRequest OrderInstance is generated
- **THEN** it progresses through the normal lifecycle (Pending → Harvested → Inspected → Shipped)

### Requirement: Staff can pause and resume any standing order before the Friday cutoff
Staff SHALL be able to manually set a standing order to Paused status (suppressing future instance generation) and release it back to Active at any time before the Friday cutoff of the affected week.

#### Scenario: Pausing a standing order stops future instance generation
- **WHEN** staff sets a standing order to Paused
- **THEN** no new instances are generated for weeks after the current week

#### Scenario: Resuming a standing order restarts normal generation
- **WHEN** staff sets a Paused standing order back to Active
- **THEN** the system resumes generating instances from the next applicable week per the standing order's frequency
