## ADDED Requirements

### Requirement: Redirect unauthenticated users to login
The system SHALL redirect any unauthenticated request to a protected Blazor route to `/account/login?returnUrl=<original-path>`.

#### Scenario: Unauthenticated access to protected page
- **WHEN** an unauthenticated user navigates to any Blazor route (e.g., `/`, `/weather`, `/counter`)
- **THEN** the system redirects the user to `/account/login?returnUrl=<requested-path>` without rendering the protected page

#### Scenario: Authenticated access to protected page
- **WHEN** an authenticated user navigates to any Blazor route
- **THEN** the system renders the requested page normally

### Requirement: Return URL must be local
The system SHALL reject any `returnUrl` that is not a local (same-origin) path to prevent open-redirect attacks.

#### Scenario: Non-local return URL is rejected
- **WHEN** the login form is submitted with a `returnUrl` pointing to an external domain
- **THEN** the system redirects to `/` instead of the supplied `returnUrl`
