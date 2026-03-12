## Why

The app currently renders with the default Bootstrap 5 stylesheet, which gives it a plain, unstyled look. Replacing it with a Bootswatch theme (a curated set of Bootstrap-compatible CSS overrides) instantly gives the UI a professional, cohesive visual identity with no JavaScript changes and minimal risk.

## What Changes

- Add the **Flatly** Bootswatch theme CSS via CDN reference in `App.razor`, replacing the local `lib/bootstrap/bootstrap.min.css` link.
- Remove the local `wwwroot/lib/bootstrap/` vendored CSS files that are no longer needed as the primary stylesheet.
- Trim `app.css` of any manual colour/button overrides that duplicate or conflict with the theme (e.g. `.btn-primary`, `a` colour, `box-shadow` focus override).
- Keep `app.css` for layout-specific rules that are not covered by Bootswatch (sidebar, content padding, validation colours, Blazor error UI).
- Update `NavMenu.razor` CSS classes to align with Flatly's navbar variant (`navbar-dark bg-primary`) so the side/top nav renders correctly.

## Capabilities

### New Capabilities

- `app-theming`: Visual theme layer for the Blazor Web frontend — defines how Bootstrap CSS is sourced (CDN vs local), which Bootswatch theme is applied, and which custom overrides in `app.css` are retained.

### Modified Capabilities

*(none — no existing spec-level behavioural requirements change)*

## Impact

- **`FarmAppAspire.Web/Components/App.razor`** — swap CDN link for Bootswatch Flatly.
- **`FarmAppAspire.Web/wwwroot/lib/bootstrap/`** — vendored Bootstrap CSS is replaced by CDN; JS bundle stays (required by Bootstrap components).
- **`FarmAppAspire.Web/wwwroot/app.css`** — remove colour/button overrides superseded by Flatly.
- **`FarmAppAspire.Web/Components/Layout/NavMenu.razor`** — adjust navbar CSS classes for Flatly palette.
- No API, database, or service changes.
- No changes to other projects in the solution.
