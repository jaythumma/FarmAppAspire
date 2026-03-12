## ADDED Requirements

### Requirement: Display login form
The system SHALL render a login page at `/account/login` containing a username field, a password field, and a submit button.

#### Scenario: Unauthenticated user visits login page
- **WHEN** an unauthenticated user navigates to `/account/login`
- **THEN** the system renders the login form with empty username and password fields

#### Scenario: Login page preserves return URL
- **WHEN** the user is redirected to `/account/login?returnUrl=%2Fweather`
- **THEN** the rendered form retains the `returnUrl` value so it is submitted with the credentials

### Requirement: Validate credentials and issue authentication cookie
The system SHALL authenticate a user by comparing submitted credentials against the configured account and, on success, issue an `HttpOnly`, `Secure`, `SameSite=Strict` authentication cookie.

#### Scenario: Correct credentials
- **WHEN** the user submits the correct username and password
- **THEN** the system issues an authentication cookie and redirects the user to the `returnUrl` (or `/` if none)

#### Scenario: Incorrect credentials
- **WHEN** the user submits an incorrect username or password
- **THEN** the system re-renders the login form with a validation error message and does NOT issue a cookie

#### Scenario: Empty fields
- **WHEN** the user submits the form with one or both fields empty
- **THEN** the system re-renders the login form with a field-level validation error and does NOT issue a cookie
