## Why

The Order Instance Administration page (`/admin/orders`) throws an unhandled HTTP 500 error on load and on every user action because all asynchronous data calls lack error handling. Any transient failure (service start-up lag, database unavailability, network error) propagates as an uncaught exception through Blazor's SSR pipeline, crashing the page rather than surfacing a friendly message. The backend browse endpoint also has an unguarded EF Core query that can fail under certain data conditions.

## What Changes

- Add `try/catch` error handling to all async methods in `Admin/Orders.razor` (`OnInitializedAsync`, `LoadBrowseAsync`, `GenerateAsync`, `PreviousWeek`, `NextWeek`) so exceptions are caught and displayed as inline error alerts rather than 500 responses.
- Add a shared `_browseError` field to surface browse failures independently from the generate workflow.
- Harden the backend `GET /admin/instances` endpoint to wrap the EF Core query in exception handling and return a structured problem response on failure instead of propagating a 500.
- Add a graceful "service unavailable" fallback display in the browse panel when the CustomerService is unreachable on initial page load.

## Capabilities

### New Capabilities

- `admin-orders-error-handling`: Resilient error display on the Order Admin page — catches and renders HTTP and unexpected errors for browse, generate, and week navigation actions without crashing the page.

### Modified Capabilities

*(none — no existing spec-level behaviour changes)*

## Impact

- **`FarmAppAspire.Web/Components/Pages/Admin/Orders.razor`** — all `@code` async methods gain `try/catch`; new `_browseError` field; browse and generate panels conditionally render error alerts.
- **`FarmAppAspire.CustomerService/Endpoints/AdminEndpoints.cs`** — `GET /admin/instances` wrapped with exception handling; returns `Results.Problem(...)` on failure.
- No API contract changes; no migrations; no new packages.
