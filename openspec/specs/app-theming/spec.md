## ADDED Requirements

### Requirement: Bootswatch Flatly theme is applied via CDN
The system SHALL load the Bootswatch Flatly Bootstrap 5 theme from the jsDelivr CDN instead of the local vendored `bootstrap.min.css` file.

#### Scenario: App.razor references Flatly CDN stylesheet
- **WHEN** the browser loads any page of `FarmAppAspire.Web`
- **THEN** the HTML `<head>` contains a `<link>` to `https://cdn.jsdelivr.net/npm/bootswatch@5/dist/flatly/bootstrap.min.css` and does NOT contain a link to `lib/bootstrap/dist/css/bootstrap.min.css`

#### Scenario: Local Bootstrap CSS link is removed
- **WHEN** `App.razor` is inspected
- **THEN** the `@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]` reference is absent

### Requirement: NavMenu renders correctly with Flatly palette
The system SHALL apply Flatly-compatible Bootstrap navbar classes to the navigation component so that the sidebar nav renders with correct colour and contrast.

#### Scenario: Navbar uses dark variant on primary background
- **WHEN** the NavMenu component is rendered
- **THEN** the `<nav>` element carries the classes `navbar-dark` and `bg-primary` (Flatly primary is `#2c3e50`)

### Requirement: app.css contains no conflicting colour overrides
The system SHALL not define manual colour or button overrides in `app.css` that duplicate or contradict the Flatly theme.

#### Scenario: Removed overrides do not appear in app.css
- **WHEN** `app.css` is inspected
- **THEN** it does NOT contain rules overriding `.btn-primary` background/border colour, the `a` link colour, or the focus `box-shadow` colour

#### Scenario: Layout and validation rules are retained
- **WHEN** `app.css` is inspected
- **THEN** it STILL contains rules for `.content`, `.sidebar`, `.top-row`, `.valid`, `.invalid`, and `#blazor-error-ui` so layout and form validation styles are unaffected
