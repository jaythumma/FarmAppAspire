## ADDED Requirements

### Requirement: Any contact at a wholesale customer may place orders
The system SHALL allow any Contact associated with a wholesale customer to be recorded as the orderer on a standing order or FedEx order instance. No authorization check beyond valid CustomerId + ContactId membership is required in this release.

#### Scenario: Contact from the wholesale customer is recorded on a standing order
- **WHEN** staff creates a standing order for a wholesale customer with a valid ContactId
- **THEN** the system associates the contact with the standing order and propagates the ContactId to generated instances

#### Scenario: Contact not belonging to the customer is rejected
- **WHEN** a ContactId is provided that does not belong to the specified CustomerId
- **THEN** the system SHALL return HTTP 400

#### Scenario: ContactId on a standing order is propagated to generated instances
- **WHEN** a StandingOrder has a ContactId set and an OrderInstance is auto-generated from it
- **THEN** the OrderInstance inherits the same ContactId

### Requirement: Retail orders do not require a contact
The system SHALL allow standing orders and FedEx order instances for retail customers to be created without a ContactId. The ContactId field SHALL be optional.

#### Scenario: Retail standing order created without a contact
- **WHEN** a standing order is submitted for a retail customer with no ContactId
- **THEN** the system creates the standing order with ContactId = null

### Requirement: ContactId can be updated on a standing order by staff
Staff SHALL be able to update the ContactId on an existing standing order (e.g. when a procurement contact leaves the company and a new one is assigned). The change applies to instances generated after the update; already-generated instances retain their original ContactId.

#### Scenario: Staff updates the contact on a standing order
- **WHEN** staff submits a contact update for a standing order with a new valid ContactId
- **THEN** the standing order's ContactId is updated; existing instances are unaffected
