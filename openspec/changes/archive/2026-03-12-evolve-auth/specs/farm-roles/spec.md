## ADDED Requirements

### Requirement: Four farm roles exist in the system
The system SHALL define and seed exactly four roles: `FarmAdmin`, `Manager`, `Staff`, and `ReadOnly`.

#### Scenario: Roles are present after first run
- **WHEN** the application has completed its startup seed
- **THEN** all four roles (`FarmAdmin`, `Manager`, `Staff`, `ReadOnly`) exist in the identity database

### Requirement: FarmAdmin has full system access
The system SHALL grant `FarmAdmin` users access to all sections including user management, customer management, and all future sections.

#### Scenario: FarmAdmin accesses user management
- **WHEN** a `FarmAdmin` navigates to `/admin/users`
- **THEN** the system renders the user management page

### Requirement: Manager has access to customers and operational data
The system SHALL grant `Manager` users full CRUD access to customer management and read access to all other sections, but NOT access to user management.

#### Scenario: Manager accesses customer management
- **WHEN** a `Manager` navigates to the customer section
- **THEN** the system renders the page normally

#### Scenario: Manager cannot access user management
- **WHEN** a `Manager` navigates to `/admin/users`
- **THEN** the system renders a "not authorized" message

### Requirement: Staff has create and view access to customers
The system SHALL grant `Staff` users the ability to view and create customer records but NOT edit, delete, or access financial or admin sections.

#### Scenario: Staff views customer list
- **WHEN** a `Staff` user navigates to the customer list
- **THEN** the system renders the list without edit or delete controls visible

### Requirement: ReadOnly users can only view data
The system SHALL grant `ReadOnly` users access to view all non-admin sections but SHALL prevent any create, edit, or delete operations.

#### Scenario: ReadOnly user attempts to create a customer
- **WHEN** a `ReadOnly` user navigates to a create-customer page
- **THEN** the system renders a "not authorized" message

### Requirement: Navigation menu reflects the user's role
The system SHALL show only navigation links that the current user's role permits.

#### Scenario: Staff user does not see admin navigation
- **WHEN** a `Staff` user is logged in
- **THEN** the navigation menu does NOT display the user management or admin section links
