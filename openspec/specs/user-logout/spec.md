## ADDED Requirements

### Requirement: Logout clears authentication cookie
The system SHALL provide a logout action that revokes the authentication cookie and redirects the user to `/account/login`.

#### Scenario: Authenticated user logs out
- **WHEN** an authenticated user triggers the logout action
- **THEN** the system revokes the authentication cookie and redirects the user to `/account/login`

#### Scenario: Logout is inaccessible when already unauthenticated
- **WHEN** an unauthenticated user accesses the logout endpoint directly
- **THEN** the system redirects to `/account/login` without error

### Requirement: Logout link visible in navigation
The system SHALL display a logout link in the navigation menu when a user is authenticated.

#### Scenario: Authenticated user sees logout link
- **WHEN** an authenticated user views any page
- **THEN** the navigation menu displays the authenticated username and a logout link

#### Scenario: Unauthenticated user does not see logout link
- **WHEN** an unauthenticated user views the login page
- **THEN** the navigation menu does NOT display a logout link
