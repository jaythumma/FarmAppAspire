## Context

The Order Admin page (`Admin/Orders.razor`) loads by calling `OnInitializedAsync → LoadBrowseAsync → OrderApi.BrowseWeekInstancesAsync`, which makes an HTTP GET to the CustomerService (`GET /admin/instances`). None of these call sites have `try/catch`. In Blazor SSR (the initial HTTP render before the interactive circuit is established), any unhandled async exception produces an HTTP 500 response instead of a rendered error UI.

The same pattern affects `GenerateAsync`, `PreviousWeek`, and `NextWeek` — all await HTTP calls without guards. The backend `GET /admin/instances` endpoint itself has no exception handling, so an EF Core query failure also bubbles as a 500 from the CustomerService.

## Goals / Non-Goals

**Goals:**
- Every async path in `Admin/Orders.razor` catches exceptions and renders an inline error alert.
- Page load never returns HTTP 500; it always renders a page, even if the data section shows an error.
- `GET /admin/instances` returns `Results.Problem(...)` instead of an unhandled 500 when the query fails.
- Browse failures and generate failures surface independently (different error fields).

**Non-Goals:**
- Retry logic or circuit-breaker UI — the `AddStandardResilienceHandler` pipeline already handles transport-level retries.
- Pagination or performance optimisation of the browse query.
- Error handling for other admin endpoints (`generate-instances` exception handling is left to the existing `UseExceptionHandler` middleware for now; the page already checks for a null response).

## Decisions

### D1 — Catch at the call site in the component, not globally

**Choice:** `try/catch` in each async method in `Orders.razor`.

**Rationale:** Blazor's SSR exception propagation happens before a circuit is available, so a global error boundary (`<ErrorBoundary>`) only works for interactive render. Catching at the call site means the component can always render HTML, differentiate between browse and generate errors, and recover gracefully when the circuit is restored.

**Alternative considered:** Wrapping `<ErrorBoundary>` around the entire page body. Rejected because it only activates after the interactive render upgrade, not on the first SSR request that triggers the 500.

### D2 — Separate browse error and generate error fields

**Choice:** `_browseError` (string?) for browse/navigation failures; existing `_generateError` for generate failures.

**Rationale:** The two panels are independent. A failed browse on initial load should not suppress the generate form, and a generate error should not clear a successfully loaded browse table.

### D3 — Backend returns `Results.Problem()` on query failure

**Choice:** Wrap the EF Core query in `GET /admin/instances` with a `try/catch (Exception ex)` that returns `Results.Problem(ex.Message, statusCode: 500)`.

**Rationale:** Consistent with ASP.NET Core's ProblemDetails convention already used elsewhere in the service (`app.UseExceptionHandler()` + `app.Services.AddProblemDetails()`). The web client (`BrowseWeekInstancesAsync`) returns an empty array on any non-success status, so the page degrades gracefully without any changes to the client code.

## Risks / Trade-offs

- **Risk:** `CancellationToken` from `_cts` is cancelled on `Dispose`. If navigation away happens mid-request, a `TaskCanceledException` is caught and shown as a browse/generate error. → **Mitigation:** Filter `OperationCanceledException` (base class of `TaskCanceledException`) — do not set the error field for cancellations; just clear the loading state.

- **Trade-off:** Inline `try/catch` in every method is repetitive. A private `ExecuteSafe(Func<Task>)` helper would DRY it up but adds indirection. Given only four call sites, inline is preferred for readability.

## Migration Plan

1. Update `AdminEndpoints.cs` — wrap `GET /admin/instances` in try/catch.
2. Update `Admin/Orders.razor` — add `_browseError`, wrap all four async methods in try/catch, render error alerts in the browse panel.
3. Run unit tests + manual smoke test (start app, navigate to `/admin/orders`, verify page loads without 500).

No database migrations, no configuration changes, no deployment steps beyond a normal redeploy.

## Open Questions

*(none)*
