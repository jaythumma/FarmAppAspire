## 1. Backend — Harden GET /admin/instances

- [x] 1.1 Wrap the EF Core query and result projection in `GET /admin/instances` with `try/catch (Exception ex)` returning `Results.Problem(ex.Message, statusCode: 500)` on failure
- [x] 1.2 Verify the endpoint still returns HTTP 200 + correct JSON on success with a manual curl/test

## 2. Blazor Component — Error State Fields

- [x] 2.1 Add `private string? _browseError` field to `Admin/Orders.razor`
- [x] 2.2 Render an inline `alert-danger` in the browse card when `_browseError` is not null (with dismiss button that clears it)

## 3. Blazor Component — Wrap Async Methods

- [x] 3.1 Wrap `LoadBrowseAsync` body in `try/catch`; set `_browseError` on exception; skip on `OperationCanceledException`
- [x] 3.2 Wrap `GenerateAsync` body in `try/catch`; set `_generateError` on exception; skip on `OperationCanceledException`
- [x] 3.3 Wrap `PreviousWeek` body in `try/catch`; set `_browseError` on exception
- [x] 3.4 Wrap `NextWeek` body in `try/catch`; set `_browseError` on exception
- [x] 3.5 Clear `_browseError` at the start of each browse action (before the try block)

## 4. Tests

- [x] 4.1 Add unit test in `FarmAppAspire.Tests.Unit` verifying `OperationCanceledException` is NOT treated as a display error (test the guard logic)
- [x] 4.2 Add unit test verifying the `admin-orders-error-handling` spec scenario: browse error does not block the generate form (independent error fields)

## 5. Verification

- [x] 5.1 Run `dotnet build` — confirm zero errors
- [x] 5.2 Run `dotnet test FarmAppAspire.Tests.Unit` — confirm all tests pass
- [ ] 5.3 Start the app via AppHost and navigate to `/admin/orders` — confirm page loads with HTTP 200
