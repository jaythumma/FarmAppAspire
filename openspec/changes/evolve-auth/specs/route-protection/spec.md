## MODIFIED Requirements

### Requirement: Redirect unauthenticated users to login
The system SHALL redirect any unauthenticated request to a protected Blazor route to `/account/login?returnUrl=<original-path>`.

#### Scenario: Unauthenticated access to protected page
- **WHEN** an unauthenticated user navigates to any Blazor route (e.g., `/`, `/weather`, `/counter`)
- **THEN** the system redirects the user to `/account/login?returnUrl=<requested-path>` without rendering the protected page

#### Scenario: Authenticated access to protected page
- **WHEN** an authenticated user navigates to any Blazor route they are authorized for
- **THEN** the system renders the requested page normally

### Requirement: Return URL must be local
The system SHALL reject any `returnUrl` that is not a local (same-origin) path to prevent open-redirect attacks.

#### Scenario: Non-local return URL is rejected
- **WHEN** the login form is submitted with a `returnUrl` pointing to an external domain
- **THEN** the system redirects to `/` instead of the supplied `returnUrl`

## ADDED Requirements

### Requirement: Role-insufficient users are shown a not-authorized page
The system SHALL render a "not authorized" view — not a redirect to login — when an authenticated user navigates to a route their role does not permit.

#### Scenario: Authenticated user accesses a role-restricted page
- **WHEN** an authenticated `Staff` user navigates to `/admin/users` (FarmAdmin only)
- **THEN** the system renders a "not authorized" message within the existing layout without redirecting to login

### Requirement: AuthorizeRouteView enforces role requirements per page
The system SHALL enforce `[Authorize(Roles = "...")]` attributes on Blazor pages, rendering the `<NotAuthorized>` fragment for both unauthenticated and insufficiently-privileged users.

#### Scenario: Unauthenticated user triggers redirect
- **WHEN** an unauthenticated user hits a role-protected page
- **THEN** the `<NotAuthorized>` fragment redirects to `/account/login`

#### Scenario: Authenticated but wrong role triggers not-authorized view
- **WHEN** an authenticated user with insufficient role hits a role-protected page
- **THEN** the `<NotAuthorized>` fragment renders an inline "not authorized" message (no redirect)
