## ADDED Requirements

### Requirement: Add an address to a customer
The system SHALL allow a Manager or FarmAdmin to add a labelled, typed address to any customer.

#### Scenario: Add billing address
- **WHEN** a Manager submits `POST /customers/{id}/addresses` with type `Billing`, a label, and full address fields
- **THEN** the system creates the address linked to the customer

#### Scenario: First address is automatically set as default
- **WHEN** a customer's first address is created
- **THEN** the system sets `IsDefault = true` on that address

#### Scenario: Address type must be valid
- **WHEN** an address is submitted with a type outside `Billing`, `Shipping`, or `Both`
- **THEN** the system returns HTTP 400

### Requirement: Retrieve addresses for a customer
The system SHALL return all addresses for a customer with the default address listed first.

#### Scenario: List addresses
- **WHEN** `GET /customers/{id}/addresses` is requested
- **THEN** the system returns all addresses, default address first

### Requirement: Set an address as the default
The system SHALL allow a Manager or FarmAdmin to designate any address as the default; the previous default SHALL be unset.

#### Scenario: Change default address
- **WHEN** a Manager sends `PUT /customers/{id}/addresses/{addressId}` with `isDefault: true`
- **THEN** the system sets that address as default and clears the `IsDefault` flag on all other addresses for the same customer

### Requirement: Update an address
The system SHALL allow a Manager or FarmAdmin to update any field of an address.

#### Scenario: Update address fields
- **WHEN** a Manager submits updated address fields via `PUT /customers/{id}/addresses/{addressId}`
- **THEN** the system updates the address record

### Requirement: Delete an address
The system SHALL allow a FarmAdmin to delete an address. Deleting the default address SHALL promote the oldest remaining address to default.

#### Scenario: Delete non-default address
- **WHEN** a FarmAdmin sends `DELETE /customers/{id}/addresses/{addressId}` for a non-default address
- **THEN** the system removes the address and returns HTTP 204

#### Scenario: Delete default address promotes next
- **WHEN** a FarmAdmin deletes the current default address and other addresses exist
- **THEN** the system removes the address and sets `IsDefault = true` on the oldest remaining address
