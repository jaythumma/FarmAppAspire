## ADDED Requirements

### Requirement: Unauthenticated users are redirected to the login page
Navigating to any protected page without an active session SHALL redirect the browser to `/account/login`.

#### Scenario: Browser navigates to home without session
- **WHEN** a browser with no session navigates to `/`
- **THEN** the browser lands on `/account/login`

### Requirement: Valid login lands on the home page with username in nav
Submitting the login form with valid credentials SHALL authenticate the user and display the home page with their username visible in the navigation bar.

#### Scenario: FarmAdmin login succeeds
- **WHEN** the login form is submitted with the seeded FarmAdmin email and password
- **THEN** the browser is on the home page and the nav bar shows the FarmAdmin's email

### Requirement: Invalid login displays an error message without authenticating
Submitting the login form with incorrect credentials SHALL display an error message and keep the user on the login page.

#### Scenario: Wrong password shows error
- **WHEN** the login form is submitted with a valid email and an incorrect password
- **THEN** the login page is still displayed and the text "Invalid email or password" is visible

### Requirement: Lockout message is shown after repeated failed login attempts
After 5 consecutive failed login attempts, the next attempt SHALL show a lockout message.

#### Scenario: Account locked after 5 failures
- **WHEN** the login form is submitted with a wrong password 5 times in a row
- **THEN** the login page displays a lockout message

### Requirement: Logout clears the session and returns the user to the login page
Clicking the logout button SHALL clear the session and redirect the browser to `/account/login`.

#### Scenario: Logout redirects to login page
- **WHEN** an authenticated user clicks the logout button in the nav bar
- **THEN** the browser navigates to `/account/login` and the username is no longer visible in the nav

### Requirement: FarmAdmin sees the Users nav link; non-admin roles do not
The "Users" navigation link in `NavMenu.razor` SHALL be visible to `FarmAdmin` users and hidden for all other roles.

#### Scenario: FarmAdmin sees Users nav link
- **WHEN** a FarmAdmin user is authenticated and on the home page
- **THEN** the "Users" nav link is visible

#### Scenario: Staff user does not see Users nav link
- **WHEN** a Staff user is authenticated and on the home page
- **THEN** the "Users" nav link is not present in the navigation

### Requirement: Non-FarmAdmin users see a Not Authorized page when accessing /admin/users
Navigating to `/admin/users` while authenticated as a non-admin role SHALL display a "not authorized" message rather than the user management table.

#### Scenario: Staff user navigates to /admin/users
- **WHEN** a Staff user navigates to `/admin/users`
- **THEN** the page displays "not authorized" text and the user management table is not visible

### Requirement: Customers nav link visible to all authenticated users
The "Customers" link in the nav SHALL be visible to any authenticated user regardless of role.

#### Scenario: Staff user sees Customers nav link
- **WHEN** a Staff user is authenticated
- **THEN** the "Customers" nav link is visible

### Requirement: Role-based visibility of Create and Edit controls on customer pages
The customer list and detail pages SHALL hide Create and Edit action controls for `Staff` and `ReadOnly` roles.

#### Scenario: FarmAdmin sees Create customer button
- **WHEN** a FarmAdmin user navigates to `/customers`
- **THEN** a Create or "New Customer" button is visible

#### Scenario: Staff user does not see Create customer button
- **WHEN** a Staff user navigates to `/customers`
- **THEN** no Create or "New Customer" button is visible

#### Scenario: ReadOnly user has no create or edit controls
- **WHEN** a ReadOnly user navigates to `/customers`
- **THEN** no Create, Edit, or Delete controls are visible anywhere on the page

### Requirement: FarmAdmin can create a customer end-to-end through the UI
A FarmAdmin user SHALL be able to create a new customer via the Create/Edit form and see it appear in the customer list.

#### Scenario: Wholesale customer created and appears in list
- **WHEN** a FarmAdmin fills in the Create Customer form with a Wholesale type and valid display name and submits
- **THEN** the browser navigates to the customer list or detail page and the new customer is visible

### Requirement: FarmAdmin can add an address to a customer and default promotion works on delete
A FarmAdmin user SHALL be able to add multiple addresses to a customer and see default address promotion when the default is deleted.

#### Scenario: Delete default address promotes next address
- **WHEN** a customer has two addresses and the FarmAdmin deletes the default one
- **THEN** the remaining address becomes the default address
