## ADDED Requirements

### Requirement: Orders board defaults to current week view
The `/orders` page SHALL default to a "This Week" view scoped to the Monday of the current week, showing only orders with a `WeekOf` within that week.

#### Scenario: User navigates to /orders
- **WHEN** a user navigates to `/orders`
- **THEN** the This Week / All toggle defaults to "This Week"
- **THEN** only orders with `WeekOf` matching the current week's Monday are shown
- **THEN** the week label displays the Monday date of the current week

### Requirement: Orders board has a This Week / All toggle
The `/orders` page SHALL provide a toggle that switches between "This Week" view (week-scoped) and "All Orders" view (unscoped, full history).

#### Scenario: User switches to All Orders view
- **WHEN** a user clicks the "All Orders" option on the toggle
- **THEN** the week navigation and Generate button are hidden
- **THEN** all orders are shown subject only to the customer, status, and channel filters

#### Scenario: User switches back to This Week view
- **WHEN** a user clicks the "This Week" option on the toggle
- **THEN** the week navigation reappears showing the current week
- **THEN** only orders for that week are shown

### Requirement: Orders board provides week navigation in This Week view
The `/orders` page SHALL provide previous-week and next-week navigation buttons and a date picker to browse any specific week when in This Week view.

#### Scenario: User navigates to previous week
- **WHEN** a user clicks the "‹ Prev" button
- **THEN** the displayed week moves back by 7 days and the order list updates to show that week's orders

#### Scenario: User navigates to next week
- **WHEN** a user clicks the "Next ›" button
- **THEN** the displayed week moves forward by 7 days and the order list updates

#### Scenario: User picks a specific week via date picker
- **WHEN** a user selects any date in the date picker
- **THEN** the page snaps to the Monday of that date's week and shows that week's orders

### Requirement: FarmAdmin can generate Insulated orders for a week from the orders board
The `/orders` page SHALL display a "Generate Insulated Orders" button visible only to FarmAdmin users when in This Week view. Clicking it generates orders for the currently displayed week using the existing admin generate endpoint.

#### Scenario: FarmAdmin generates orders for the current week
- **WHEN** a FarmAdmin clicks "Generate Insulated Orders" while in This Week view
- **THEN** the generate endpoint is called with the displayed week's date
- **THEN** the order list refreshes to show the newly created orders
- **THEN** a summary is displayed showing how many orders were created and how many were skipped

#### Scenario: Generate is clicked for a week that already has orders
- **WHEN** a FarmAdmin clicks "Generate Insulated Orders" for a week where orders already exist
- **THEN** the endpoint responds with skipped count = total (idempotent)
- **THEN** a summary message communicates that orders were already up to date

#### Scenario: Non-FarmAdmin views the orders board in This Week mode
- **WHEN** a non-FarmAdmin user views `/orders` in This Week view
- **THEN** the "Generate Insulated Orders" button is not visible

### Requirement: Orders board accepts customer and channel filter from incoming navigation
The `/orders` page SHALL read `customerId` and `channel` query parameters from the URL and pre-populate the corresponding filters when present.

#### Scenario: User navigates from Customer Detail "View all orders" link
- **WHEN** a user clicks "View all orders →" from a Customer Detail card that includes `?customerId=<id>&channel=Insulated`
- **THEN** the orders board pre-filters to that customer and channel in All Orders view

### Requirement: `/standing-orders` redirects to `/orders`
The `/standing-orders` route SHALL redirect to `/orders` so that any existing bookmarks or links are not broken.

#### Scenario: User navigates to /standing-orders
- **WHEN** a user navigates to `/standing-orders`
- **THEN** they are redirected to `/orders`
