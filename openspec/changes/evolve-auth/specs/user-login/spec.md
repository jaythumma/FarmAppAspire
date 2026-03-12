## MODIFIED Requirements

### Requirement: Validate credentials and issue authentication cookie
The system SHALL authenticate a user by verifying submitted credentials against the ASP.NET Core Identity store via `SignInManager`, and on success issue an `HttpOnly`, `Secure`, `SameSite=Strict` authentication cookie containing the user's `Id` (GUID) as the `NameIdentifier` claim and the user's email as the `Name` claim.

#### Scenario: Correct credentials
- **WHEN** the user submits a valid email and password matching an active Identity account
- **THEN** the system issues an authentication cookie and redirects the user to the `returnUrl` (or `/` if none)

#### Scenario: Incorrect credentials
- **WHEN** the user submits an email or password that does not match any active Identity account
- **THEN** the system re-renders the login form with a validation error message and does NOT issue a cookie

#### Scenario: Empty fields
- **WHEN** the user submits the form with one or both fields empty
- **THEN** the system re-renders the login form with a field-level validation error and does NOT issue a cookie

#### Scenario: Deactivated account
- **WHEN** the user submits correct credentials for a deactivated account
- **THEN** the system re-renders the login form with an "account is inactive" error and does NOT issue a cookie

#### Scenario: Account locked out after repeated failures
- **WHEN** the user submits incorrect credentials more than 5 times consecutively
- **THEN** the system locks the account for 15 minutes and renders a lockout message
