## ADDED Requirements

### Requirement: Login page is accessible without authentication
The `GET /account/login` endpoint SHALL return HTTP 200 for unauthenticated requests.

#### Scenario: Login page renders for unauthenticated user
- **WHEN** an unauthenticated HTTP GET request is made to `/account/login`
- **THEN** response is HTTP 200

### Requirement: Protected routes redirect unauthenticated users to login
Any route protected by the `[Authorize]` attribute or the fallback authorization policy SHALL redirect unauthenticated requests to `/account/login`.

#### Scenario: Unauthenticated access to home page redirects to login
- **WHEN** an unauthenticated HTTP GET request is made to `/`
- **THEN** the response redirects (HTTP 302) to `/account/login`

### Requirement: Valid credentials result in an auth cookie and redirect
Submitting the login form with valid Identity credentials SHALL issue an authentication cookie and redirect to the return URL or home page.

#### Scenario: Successful login sets auth cookie
- **WHEN** `POST /account/login` is submitted with the seeded FarmAdmin email and password
- **THEN** response is a redirect and the `Set-Cookie` header contains the Identity auth cookie

### Requirement: Invalid credentials are rejected without issuing a cookie
Submitting the login form with an unrecognised email or wrong password SHALL not issue an auth cookie and SHALL re-render the login page.

#### Scenario: Wrong password rejected
- **WHEN** `POST /account/login` is submitted with a valid email but incorrect password
- **THEN** response does not contain a `Set-Cookie` auth cookie and the response body contains the login form

### Requirement: Role-restricted routes return 403 for authenticated users with insufficient role
A route decorated with `[Authorize(Roles = "FarmAdmin")]` SHALL return HTTP 403 for authenticated users who do not hold that role.

#### Scenario: Staff user denied access to admin/users
- **WHEN** an HTTP GET to `/admin/users` is made by an authenticated Staff user (auth cookie present, role = Staff)
- **THEN** response is HTTP 403 or redirects to the not-authorized page

### Requirement: Logout clears the authentication cookie
`POST /account/logout` SHALL sign out the current user and redirect to `/account/login`, with the authentication cookie cleared.

#### Scenario: Logout removes auth cookie
- **WHEN** an authenticated user posts to `/account/logout`
- **THEN** the response redirects to `/account/login` and the `Set-Cookie` header expires the auth cookie
