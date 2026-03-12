## ADDED Requirements

### Requirement: Create a customer record
The system SHALL allow an authenticated user (Manager or FarmAdmin) to create a customer of type Wholesale or Retail, providing at minimum a display name and type.

#### Scenario: Create wholesale customer
- **WHEN** a Manager submits a new customer with type `Wholesale`, a company name, and optional tax ID and payment terms
- **THEN** the system creates the record, sets `CreatedBy` to the caller's `X-User-Id`, and returns the new customer with a generated GUID

#### Scenario: Create retail customer
- **WHEN** a Manager submits a new customer with type `Retail` and a display name
- **THEN** the system creates the record with `CompanyName`, `TaxId`, and `PaymentTerms` as null

#### Scenario: Display name is required
- **WHEN** a request to create a customer is submitted without a display name
- **THEN** the system returns HTTP 400 with a validation error

### Requirement: Retrieve a customer record
The system SHALL allow any authenticated user to retrieve a single customer by ID or a paginated list of all customers.

#### Scenario: Get customer by ID
- **WHEN** a valid customer ID is requested via `GET /customers/{id}`
- **THEN** the system returns the customer record including its contacts and addresses

#### Scenario: Get customer list with pagination
- **WHEN** `GET /customers?page=1&size=25` is requested
- **THEN** the system returns up to 25 customer records and a total count

#### Scenario: Customer not found
- **WHEN** a non-existent customer ID is requested
- **THEN** the system returns HTTP 404

### Requirement: Update a customer record
The system SHALL allow a Manager or FarmAdmin to update a customer's fields; `ModifiedBy` and `ModifiedAt` SHALL be updated on every change.

#### Scenario: Update customer fields
- **WHEN** a Manager submits a `PUT /customers/{id}` with changed fields
- **THEN** the system updates the record and sets `ModifiedBy` to the caller's `X-User-Id`

### Requirement: Delete a customer record
The system SHALL allow a FarmAdmin to delete a customer; all associated contacts and addresses SHALL be cascade-deleted.

#### Scenario: Delete customer
- **WHEN** a FarmAdmin sends `DELETE /customers/{id}`
- **THEN** the system removes the customer, all contacts, and all addresses, returning HTTP 204

#### Scenario: Non-FarmAdmin cannot delete
- **WHEN** a Manager or lower sends `DELETE /customers/{id}`
- **THEN** the system returns HTTP 403
