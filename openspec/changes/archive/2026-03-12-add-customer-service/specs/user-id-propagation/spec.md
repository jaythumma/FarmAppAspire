## ADDED Requirements

### Requirement: X-User-Id header is injected on all CustomerService requests
The system SHALL forward the authenticated user's Identity GUID as the `X-User-Id` header on every HTTP request from `webfrontend` to `CustomerService`, using a `DelegatingHandler` registered on the `CustomerApiClient` typed HTTP client.

#### Scenario: Authenticated request includes user ID header
- **WHEN** an authenticated user triggers any customer API call from the web frontend
- **THEN** the outbound HTTP request to CustomerService contains an `X-User-Id` header with the user's Identity GUID

#### Scenario: Unauthenticated request does not reach CustomerService
- **WHEN** an unauthenticated user attempts to access a customer page
- **THEN** the route protection redirects to login before any CustomerService API call is made

### Requirement: CustomerService reads X-User-Id for audit fields
The system SHALL read the `X-User-Id` header on every mutating request (POST, PUT, DELETE) and record the value as `CreatedBy` or `ModifiedBy` on the affected entity.

#### Scenario: Create sets CreatedBy
- **WHEN** `POST /customers` is received with a valid `X-User-Id` header
- **THEN** the new customer record has `CreatedBy` set to the value of `X-User-Id`

#### Scenario: Update sets ModifiedBy
- **WHEN** `PUT /customers/{id}` is received with a valid `X-User-Id` header
- **THEN** the customer record has `ModifiedBy` and `ModifiedAt` updated to the caller's ID and current UTC time

#### Scenario: Missing X-User-Id returns 400
- **WHEN** a mutating request arrives at CustomerService without the `X-User-Id` header
- **THEN** the system returns HTTP 400 with an error indicating the header is required
