## ADDED Requirements

### Requirement: Order instances are auto-generated weekly from active standing orders
A background service SHALL generate OrderInstance records each week for all Active standing orders whose frequency schedule includes that Monday. Generation SHALL be idempotent — if an instance already exists for a given StandingOrder and WeekOf, no duplicate is created.

#### Scenario: Instance generated for active weekly standing order
- **WHEN** the generation service runs and a weekly standing order is Active with no instance for the current Monday
- **THEN** a new OrderInstance with Status=Pending is created for that standing order and WeekOf=current Monday

#### Scenario: Duplicate instance not created
- **WHEN** the generation service runs and an instance already exists for a standing order's current WeekOf
- **THEN** no second instance is created

#### Scenario: Paused standing orders are skipped
- **WHEN** the generation service runs and a standing order has Status=Paused
- **THEN** no instance is generated for that standing order that week

### Requirement: The harvest calendar runs Thursday through Monday
The farm's production window for each weekly shipment spans Thursday through Monday. Harvesting and processing (wash, dry) begins Thursday. USDA inspections occur on Fridays and Mondays only. Some packed boxes ship Friday afternoon (after Friday inspection). The majority ship Monday.

#### Scenario: USDA inspection only occurs on Fridays and Mondays
- **WHEN** staff marks an instance as Inspected on any day other than Friday or Monday
- **THEN** the system SHALL return HTTP 422

### Requirement: Order instance status progresses through a fixed lifecycle
OrderInstance statuses SHALL follow the sequence: Pending → Harvested → Inspected → Shipped, or Pending → Cancelled. Backward transitions and skipped transitions SHALL be rejected.

#### Scenario: Valid forward transition is accepted
- **WHEN** staff transitions an instance from Pending to Harvested
- **THEN** the system updates the status and records the timestamp

#### Scenario: Invalid backward transition is rejected
- **WHEN** staff attempts to set a Shipped instance back to Inspected
- **THEN** the system SHALL return HTTP 422

#### Scenario: Cancellation is only valid from Pending status
- **WHEN** staff cancels an instance that is already Harvested or further
- **THEN** the system SHALL return HTTP 422

### Requirement: USDA inspection failure results in discard and fulfillment with fresh leaf
When a USDA inspection fails, the affected leaf is discarded. The OrderInstance SHALL remain in the current processing cycle and be fulfilled using freshly harvested, inspection-passed leaf. The instance is NOT cancelled and the customer is NOT notified. Orders are always fulfilled.

#### Scenario: Inspection failure is recorded but instance continues
- **WHEN** staff records a USDA inspection failure for a batch associated with an instance
- **THEN** the system logs the failure; the instance remains in Harvested status pending a fresh inspection pass

#### Scenario: Instance advances to Inspected only after a passing result
- **WHEN** staff records a passing USDA inspection result
- **THEN** the instance is transitioned to Inspected status

### Requirement: Instances ship on Friday or Monday; invoice is generated at Shipped
Staff SHALL record the actual ship date when marking an instance as Shipped. The ship date SHALL be the date the transition occurs (Friday or Monday). An Invoice SHALL be created immediately when IsSample=false and the instance transitions to Shipped.

#### Scenario: Invoice is created when a non-sample instance is shipped
- **WHEN** an instance with IsSample=false is transitioned to Shipped
- **THEN** the system creates an Invoice record with the correct label, SeasonYear, and SeekNum

#### Scenario: Sample instance shipped generates no invoice
- **WHEN** an instance with IsSample=true is transitioned to Shipped
- **THEN** no Invoice record is created
