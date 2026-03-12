## ADDED Requirements

### Requirement: Customer list page is accessible to all authenticated farm roles
The system SHALL provide a customer list page at `/customers` showing all customers with search and type filter.

#### Scenario: Staff user views customer list
- **WHEN** a `Staff` user navigates to `/customers`
- **THEN** the system renders a paginated list of all customers without edit or delete controls

#### Scenario: Manager views customer list with actions
- **WHEN** a `Manager` navigates to `/customers`
- **THEN** the system renders the list with edit and view detail actions visible

#### Scenario: ReadOnly user can view but not create
- **WHEN** a `ReadOnly` user views the customer list
- **THEN** no "Create Customer" button is visible

### Requirement: Customer detail page shows contacts and addresses
The system SHALL provide a customer detail page showing all customer fields, a contacts section, and an addresses section.

#### Scenario: Manager views customer detail
- **WHEN** a `Manager` navigates to `/customers/{id}`
- **THEN** the system renders the customer's type, fields, all contacts with roles, and all addresses with types and default indicator

### Requirement: Create and edit customer forms are accessible to Manager and above
The system SHALL provide create and edit forms that respect the role permission matrix.

#### Scenario: Manager creates a wholesale customer
- **WHEN** a `Manager` completes the create customer form with type `Wholesale`
- **THEN** the system shows company-specific fields (Company Name, Tax ID, Payment Terms) in addition to shared fields

#### Scenario: Retail customer form hides wholesale-only fields
- **WHEN** a user selects type `Retail` on the create form
- **THEN** the fields `Company Name`, `Tax ID`, and `Payment Terms` are hidden

#### Scenario: Staff cannot access create form
- **WHEN** a `Staff` user navigates to `/customers/create`
- **THEN** the system renders a "not authorized" message

### Requirement: Customer management nav link is visible to all authenticated roles
The system SHALL display a customer management link in the navigation menu for all authenticated users.

#### Scenario: All authenticated users see customer nav link
- **WHEN** any authenticated user is logged in
- **THEN** the navigation menu displays a "Customers" link
