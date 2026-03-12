## ADDED Requirements

### Requirement: Add a contact to a customer
The system SHALL allow a Manager or FarmAdmin to add a contact to any customer, assigning a role from the defined set.

#### Scenario: Add primary contact to retail customer
- **WHEN** a Manager submits `POST /customers/{id}/contacts` with `role: Primary`, first name, last name, and email
- **THEN** the system creates the contact linked to the customer and returns HTTP 201

#### Scenario: Add billing contact to wholesale customer
- **WHEN** a Manager submits a contact with `role: Billing` for a wholesale customer
- **THEN** the system creates the contact with the Billing role

#### Scenario: Contact role must be valid
- **WHEN** a contact is submitted with a role value outside the allowed set
- **THEN** the system returns HTTP 400 with a validation error

### Requirement: Retrieve contacts for a customer
The system SHALL return all contacts for a given customer ordered by role priority (Primary first).

#### Scenario: List contacts
- **WHEN** `GET /customers/{id}/contacts` is requested
- **THEN** the system returns all contacts for the customer, Primary contact first

### Requirement: Update a contact
The system SHALL allow a Manager or FarmAdmin to update any field of a contact.

#### Scenario: Update contact email
- **WHEN** a Manager submits `PUT /customers/{id}/contacts/{contactId}` with a new email address
- **THEN** the system updates the contact's email field

### Requirement: Delete a contact
The system SHALL allow a FarmAdmin to delete a contact from a customer.

#### Scenario: Delete contact
- **WHEN** a FarmAdmin sends `DELETE /customers/{id}/contacts/{contactId}`
- **THEN** the system removes the contact and returns HTTP 204
