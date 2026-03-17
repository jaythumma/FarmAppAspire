## ADDED Requirements

### Requirement: FedEx orders card is shown on Customer Detail
The Customer Detail page SHALL display a FedEx Orders card for customers whose channel type includes FedEx, showing recent ad-hoc orders and an action to create a new one.

#### Scenario: Customer has FedEx orders
- **WHEN** a user navigates to Customer Detail for a customer with past FedEx orders
- **THEN** a FedEx Orders card is displayed listing the 5 most recent FedEx orders with date, line summary, and status badge

#### Scenario: Customer has no FedEx orders yet
- **WHEN** a user navigates to Customer Detail for a FedEx-channel customer with no prior orders
- **THEN** a FedEx Orders card is shown with an empty state and a "New FedEx Order" button

#### Scenario: Customer Detail — navigate to full FedEx order history
- **WHEN** a user clicks "View all orders →" in the FedEx Orders card
- **THEN** the user is navigated to `/orders` filtered by this customer and the FedEx channel

### Requirement: FedEx order is created without a StandingOrder
The system SHALL create a FedEx Order with a null `StandingOrderId`. No StandingOrder SHALL be created or looked up during FedEx order creation.

#### Scenario: FedEx order creation succeeds
- **WHEN** a valid FedEx order creation request is submitted for a customer
- **THEN** an Order is persisted with `Channel = FedEx` and `StandingOrderId = null`
- **THEN** no StandingOrder record is created or modified

#### Scenario: Multiple FedEx orders for the same customer
- **WHEN** a second FedEx order is created for the same customer
- **THEN** it is created independently with `StandingOrderId = null`, regardless of any existing FedEx orders

### Requirement: FedEx order creation is initiated from Customer Detail
FarmAdmin users SHALL be able to initiate FedEx order creation from the FedEx Orders card on Customer Detail, with the customer pre-populated.

#### Scenario: Admin creates a FedEx order from Customer Detail
- **WHEN** a FarmAdmin clicks "New FedEx Order" on the FedEx Orders card
- **THEN** the user is navigated to the order creation form with the customer and channel pre-populated as FedEx

#### Scenario: Non-admin views FedEx Orders card
- **WHEN** a non-FarmAdmin user views the FedEx Orders card
- **THEN** the "New FedEx Order" button is not shown
- **THEN** the order history list is still visible
