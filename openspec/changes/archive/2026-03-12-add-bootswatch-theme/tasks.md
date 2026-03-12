## 1. Swap Bootstrap CSS for Bootswatch Flatly CDN

- [x] 1.1 In `FarmAppAspire.Web/Components/App.razor`, replace the `<link rel="stylesheet" href="@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]" />` line with `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootswatch@5/dist/flatly/bootstrap.min.css" />`

## 2. Prune Conflicting Overrides from app.css

- [x] 2.1 In `FarmAppAspire.Web/wwwroot/app.css`, remove the `.btn-primary` rule block (background-color and border-color overrides)
- [x] 2.2 Remove the `a, .btn-link { color: #006bb7; }` rule — Flatly owns link colour
- [x] 2.3 Remove the focus `box-shadow` override in `.btn:focus, .btn:active:focus, ...` — Flatly provides its own focus ring
- [x] 2.4 Verify that `.content`, `.sidebar`, `.top-row`, `.valid`, `.invalid`, `#blazor-error-ui`, and validation-colour rules are all still present

## 3. Update NavMenu for Flatly Palette

- [x] 3.1 In `FarmAppAspire.Web/Components/Layout/NavMenu.razor`, locate the `<nav>` element and ensure it carries `navbar-dark bg-primary` classes so the sidebar renders with Flatly's dark primary colour (`#2c3e50`)

## 4. Verification

- [x] 4.1 Run `dotnet run --project FarmAppAspire.AppHost` and confirm the home page renders with the Flatly theme (flat green/teal nav, clean typography)
- [x] 4.2 Navigate to `/customers` and confirm the customer list table and buttons use Flatly styling
- [x] 4.3 Navigate to `/account/login` and confirm the login form renders cleanly with Flatly inputs and button
- [x] 4.4 Navigate to `/admin/users` as FarmAdmin and confirm the user management table and badge colours are readable
- [x] 4.5 Confirm no console errors related to missing CSS or blocked CDN requests
- [x] 4.6 Run `dotnet test FarmAppAspire.Tests` — confirm all existing tests still pass (theme change is UI-only, no logic change)
