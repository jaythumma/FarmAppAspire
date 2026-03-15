## ADDED Requirements

### Requirement: Add contact to a customer
The system SHALL allow an authorized FarmAdmin user to add a new contact to a customer from the Customer Detail page. The form SHALL capture: first name (required), last name (required), role (required, default Primary), email (optional), phone (optional), mobile (optional), and primary flag (optional boolean). After a successful save the new contact SHALL appear in the contacts table without a full page reload.

#### Scenario: Wholesale customer adds first contact
- **WHEN** a FarmAdmin views a Wholesale customer detail page with no existing contacts
- **THEN** a "No contacts" hint message and an "Add Contact" button SHALL be displayed
- **WHEN** the FarmAdmin clicks "Add Contact"
- **THEN** an inline contact form SHALL appear with Role defaulting to Primary
- **WHEN** the FarmAdmin submits the form with valid first name, last name, and role
- **THEN** the new contact SHALL appear in the contacts table and the inline form SHALL close

#### Scenario: Form validation prevents submission with missing required fields
- **WHEN** the FarmAdmin submits the add-contact form with an empty first name or last name
- **THEN** the form SHALL display a validation error and NOT call the API

#### Scenario: API error surfaces inline
- **WHEN** the API call to create a contact returns a non-success status code
- **THEN** an inline error alert SHALL be displayed describing the failure and the form SHALL remain open

### Requirement: Edit an existing contact
The system SHALL allow a FarmAdmin user to edit any existing contact on the Customer Detail page. Clicking an "Edit" button on a contact row SHALL open the inline form pre-populated with that contact's current data. Submitting the form SHALL call the update endpoint and refresh the contact row in place.

#### Scenario: Edit contact updates the contacts table
- **WHEN** the FarmAdmin clicks "Edit" on an existing contact row
- **THEN** the inline form SHALL open with all fields populated from that contact's current data
- **WHEN** the FarmAdmin changes the role and submits
- **THEN** the contact row in the table SHALL reflect the new role and the form SHALL close

#### Scenario: Cancel edit discards changes
- **WHEN** the FarmAdmin clicks "Cancel" while the edit form is open
- **THEN** the form SHALL close and the contacts table SHALL be unchanged

### Requirement: Delete a contact
The system SHALL allow a FarmAdmin user to delete a contact from the Customer Detail page. A two-step confirmation SHALL be required before the DELETE API call is made, to prevent accidental removal.

#### Scenario: Delete contact with confirmation
- **WHEN** the FarmAdmin clicks "Delete" on a contact row
- **THEN** inline "Confirm?" and "Cancel" controls SHALL appear for that row
- **WHEN** the FarmAdmin clicks "Confirm?"
- **THEN** the contact SHALL be removed from the API and disappear from the contacts table
- **WHEN** the FarmAdmin clicks "Cancel" instead
- **THEN** the confirmation controls SHALL disappear and the row SHALL remain unchanged

### Requirement: Wholesale contact hint
The system SHALL display a visible hint on the Customer Detail page when a Wholesale customer has zero contacts, indicating that adding a contact is recommended.

#### Scenario: Wholesale customer with no contacts shows hint
- **WHEN** a user views the detail page of a Wholesale customer with no contacts
- **THEN** a hint message SHALL be displayed (e.g., "Wholesale customers should have at least one contact")

#### Scenario: Retail customer with no contacts does not show hint
- **WHEN** a user views the detail page of a Retail customer with no contacts
- **THEN** no contact-required hint SHALL be displayed

### Requirement: Default contact role
The system SHALL default the role selection to Primary when no role has been explicitly chosen by the user in the contact form.

#### Scenario: Add contact form defaults role to Primary
- **WHEN** the FarmAdmin opens the add-contact form
- **THEN** the Role field SHALL default to Primary

### Requirement: CustomerApiClient contact methods
The `CustomerApiClient` SHALL expose three async methods for contact management: `CreateContactAsync`, `UpdateContactAsync`, and `DeleteContactAsync`, targeting the existing backend endpoints.

#### Scenario: CreateContactAsync returns ContactDto on success
- **WHEN** `CreateContactAsync` is called with a valid `CreateContactRequest` and the API returns HTTP 201
- **THEN** the method SHALL return the deserialized `ContactDto`

#### Scenario: UpdateContactAsync returns ContactDto on success
- **WHEN** `UpdateContactAsync` is called with a valid `UpdateContactRequest` and the API returns HTTP 200
- **THEN** the method SHALL return the deserialized `ContactDto`

#### Scenario: DeleteContactAsync returns true on HTTP 204
- **WHEN** `DeleteContactAsync` is called and the API returns HTTP 204
- **THEN** the method SHALL return `true`
