## MODIFIED Requirements

### Requirement: Create a customer record
The system SHALL allow an authenticated user (Manager or FarmAdmin) to create a customer of type Wholesale or Retail, providing at minimum a display name, type, and shipping address. For Wholesale customers, `CompanyName` is also required.

#### Scenario: Create wholesale customer
- **WHEN** a Manager submits a new customer with type `Wholesale`, a company name, and a shipping address
- **THEN** the system creates the customer record, creates the shipping address linked to the customer, sets `CreatedBy` to the caller's `X-User-Id`, and returns the new customer detail

#### Scenario: Create wholesale customer without CompanyName returns 400
- **WHEN** a request is submitted with `type: Wholesale` and `companyName` is null or empty
- **THEN** the system returns HTTP 400 with a validation error identifying `CompanyName` as required

#### Scenario: Create retail customer
- **WHEN** a Manager submits a new customer with type `Retail`, a display name, and a shipping address
- **THEN** the system creates the record with `CompanyName`, `TaxId`, and `PaymentTerms` as null

#### Scenario: Create customer without shipping address returns 400
- **WHEN** a request to create a customer is submitted without a `shippingAddress`
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Display name is required
- **WHEN** a request to create a customer is submitted without a display name
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Create customer with billingUsesShipping true
- **WHEN** a customer is created with `billingUsesShipping: true` (or using the default)
- **THEN** the system creates one Shipping address record and no Billing address record; `BillingUsesShipping` is stored as `true`

#### Scenario: Create customer with explicit billing address
- **WHEN** a customer is created with `billingUsesShipping: false` and a valid `billingAddress`
- **THEN** the system creates both a Shipping and a Billing address record; `BillingUsesShipping` is stored as `false`

## MODIFIED Requirements

### Requirement: Update a customer record
The system SHALL allow a Manager or FarmAdmin to update a customer's fields; `ModifiedBy` and `ModifiedAt` SHALL be updated on every change. `CompanyName` is required when the customer type is `Wholesale`.

#### Scenario: Update wholesale customer — CompanyName required
- **WHEN** a Manager submits `PUT /customers/{id}` for a Wholesale customer with `companyName` empty or null
- **THEN** the system returns HTTP 400

#### Scenario: Update customer fields
- **WHEN** a Manager submits a `PUT /customers/{id}` with changed fields and a valid `CompanyName` for Wholesale
- **THEN** the system updates the record and sets `ModifiedBy` to the caller's `X-User-Id`
