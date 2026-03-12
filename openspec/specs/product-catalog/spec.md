## ADDED Requirements

### Requirement: Product catalog defines CurryLeaf with two fulfillment channels
The system SHALL define a single product (CurryLeaf) that is sold through two mutually exclusive channels: Insulated (direct farm shipment) and FedEx (Amazon/external channel). The two channels have distinct box categories, sizing units, and pricing models that SHALL NOT be substituted for one another.

#### Scenario: CurryLeaf is the only product
- **WHEN** the system presents orderable products
- **THEN** only CurryLeaf is available

#### Scenario: Channels are non-interchangeable
- **WHEN** a customer has a FedEx channel order
- **THEN** the system SHALL NOT allow switching it to insulated pricing or box sizes

### Requirement: Insulated box category defines three sizes with pound-based pricing
The system SHALL support three insulated box sizes — 5 lb, 10 lb, and 12 lb — with a base price of $13.00 per lb. The 12 lb box SHALL be the default. All insulated orders SHALL be expressed in multiples of these box sizes (e.g. three 10 lb boxes = 30 lb).

#### Scenario: Default box size is 12 lb
- **WHEN** a new order line is created without specifying a box size
- **THEN** the system defaults to the 12 lb insulated box

#### Scenario: Base pricing applied per pound
- **WHEN** an insulated order line for `N` boxes of size `S` lb is priced at the base rate
- **THEN** the line total = `N × S × $13.00`

#### Scenario: Only supported sizes are accepted
- **WHEN** a request is made with an insulated box size other than 5 lb, 10 lb, or 12 lb
- **THEN** the system SHALL return HTTP 400

### Requirement: FedEx box category defines eight oz-tier sizes with fixed pricing
The system SHALL support eight FedEx tier sizes with fixed, non-negotiable prices:

| Size | Price |
|---|---|
| 1 oz | $5.99 |
| 2 oz | $8.99 |
| 4 oz | $12.99 |
| 8 oz | $19.99 |
| 1 lb | $27.99 |
| 2 lb | $42.99 |
| 3 lb | $68.99 |
| 5 lb | $99.99 |

#### Scenario: FedEx tier price is always fixed
- **WHEN** a FedEx order is created for any tier size
- **THEN** the price is the fixed tier amount and cannot be overridden

#### Scenario: FedEx packaging threshold
- **WHEN** a FedEx order is for a quantity whose total weight is less than 2 lb
- **THEN** the system SHALL record packaging type as Envelope
- **WHEN** the total weight is 2 lb or more
- **THEN** the system SHALL record packaging type as Box

### Requirement: Box configurations are system-defined and not user-editable
The system SHALL seed insulated box sizes and FedEx tier prices as static configuration. Only a code migration SHALL change these values.

#### Scenario: Admin cannot modify FedEx tier prices through the UI
- **WHEN** an Admin or Manager accesses pricing settings
- **THEN** FedEx tier prices are read-only and cannot be altered via the management UI
