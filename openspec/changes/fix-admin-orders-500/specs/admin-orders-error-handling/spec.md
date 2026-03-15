## ADDED Requirements

### Requirement: Order Admin page loads without HTTP 500 on service failure
When the CustomerService is unavailable or returns an error during the initial page load, the Order Admin page SHALL render successfully (HTTP 200) and display an inline error alert in the browse panel rather than returning an HTTP 500 response.

#### Scenario: Browse fails on initial load due to service unavailability
- **WHEN** the user navigates to `/admin/orders` and the CustomerService is unreachable
- **THEN** the page SHALL load with HTTP 200, display the generate form, and show an error alert in the browse panel reading the failure message

#### Scenario: Browse fails on initial load due to unexpected exception
- **WHEN** `GET /admin/instances` returns a 500 from the CustomerService
- **THEN** the page SHALL load with HTTP 200 and display an inline browse error alert without crashing the page

### Requirement: Generate action surfaces errors inline without crashing
When the generate-instances call fails, the Order Admin page SHALL display an inline error alert in the generate section and remain fully interactive.

#### Scenario: Generate call throws an HTTP exception
- **WHEN** the user clicks "Generate Instances" and the HTTP call throws
- **THEN** the generate error alert SHALL appear with the exception message and the loading spinner SHALL be cleared

#### Scenario: Generate call is cancelled by navigation
- **WHEN** the user navigates away while a generate call is in-flight
- **THEN** no error alert SHALL be shown (cancellation is silent)

### Requirement: Week navigation surfaces errors inline without crashing
When the previous/next week browse calls fail, the Order Admin page SHALL display an inline browse error alert and leave the current browse data intact.

#### Scenario: Next/Prev week browse call throws
- **WHEN** the user clicks "Next" or "Prev" and the HTTP call to `GET /admin/instances` throws
- **THEN** an inline browse error alert SHALL appear with the error message; previously loaded browse data SHALL remain visible

### Requirement: Backend browse endpoint returns structured error on query failure
The `GET /admin/instances` endpoint in CustomerService SHALL return an RFC 7807 Problem Details response (HTTP 500 with `application/problem+json` body) when the database query fails, rather than propagating an unhandled exception.

#### Scenario: EF Core query throws during browse
- **WHEN** the database query in `GET /admin/instances` throws an exception
- **THEN** the endpoint SHALL return HTTP 500 with a Problem Details JSON body containing the error detail

#### Scenario: Successful browse returns instance list
- **WHEN** `GET /admin/instances?weekOf=2025-04-07` is called and the query succeeds
- **THEN** the endpoint SHALL return HTTP 200 with a JSON array of `WeekInstanceSummary` objects for the Monday of the specified week
