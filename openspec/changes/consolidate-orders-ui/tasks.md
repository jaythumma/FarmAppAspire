## 1. Backend — Data Model & API

- [x] 1.1 Add EF Core migration: make `Order.StandingOrderId` nullable (`Guid?`) in `FarmAppAspire.CustomerService`
- [x] 1.2 Update `Order` model: change `StandingOrderId` to `Guid?` and `StandingOrder` navigation property to nullable
- [x] 1.3 Remove `OrderFrequency.OnRequest` from the enum; verify no remaining usages in production code
- [x] 1.4 Update `AdminEndpoints.generate-orders`: remove `Frequency != OrderFrequency.Stopped` filter clause that referenced `OnRequest` if present
- [x] 1.5 Update `POST /customers/{customerId}/orders` in `OrderEndpoints`: remove the FedEx branch that finds-or-creates a StandingOrder; FedEx orders now set `StandingOrderId = null` directly
- [x] 1.6 Update `AllOrdersSummaryDto` and `OrderDto` to reflect nullable `StandingOrderId` (change to `Guid?`)
- [x] 1.7 Update `OrderApiClient` in `FarmAppAspire.Web` to handle nullable `StandingOrderId` in deserialized DTOs

## 2. Backend — Tests

- [x] 2.1 Update `AdminGenerateOrdersTests` — remove assertions that rely on `OrderFrequency.OnRequest`
- [x] 2.2 Update FedEx order creation unit tests: assert `StandingOrderId == null` and that no StandingOrder is created
- [x] 2.3 Add unit test: creating multiple FedEx orders for the same customer does not create any StandingOrders
- [x] 2.4 Verify existing Insulated order creation tests still pass (StandingOrderId still required for Insulated)
- [x] 2.5 Run full test suite; confirm all tests pass

## 3. Customer Detail — Insulated Schedule Card

- [x] 3.1 Add API call in `CustomerApiClient` (or reuse existing) to fetch a customer's Insulated StandingOrder by customerId
- [x] 3.2 Add Insulated Schedule card section to `Customers/Detail.razor` — displays status badge, frequency, season year, box lines, estimated totals, and skip list
- [x] 3.3 Add inline edit form inside the card (FarmAdmin only) for frequency, monthly week (conditional), and box lines — calls existing PUT endpoint
- [x] 3.4 Add Pause / Resume / Stop actions to the card (FarmAdmin only) with confirmation prompt for Stop — calls existing PATCH endpoints
- [x] 3.5 Add "Add Skip" action to the card (FarmAdmin only) with date picker — calls existing skip endpoint; enforce cutoff rule via `StandingOrderRules.CanAddSkip`
- [x] 3.6 Show empty-state card with "Create Schedule" link for FarmAdmin when no Insulated SO exists
- [x] 3.7 Fetch and display 5 most recent Insulated orders in the card — calls existing orders endpoint filtered by customer + channel
- [x] 3.8 Add "View all orders →" link that navigates to `/orders?customerId={id}&channel=Insulated`

## 4. Customer Detail — FedEx Orders Card

- [x] 4.1 Add FedEx Orders card section to `Customers/Detail.razor` — shown for customers whose `ChannelType` includes FedEx
- [x] 4.2 Fetch and display 5 most recent FedEx orders in the card (reuse orders API, filter `channel=FedEx`)
- [x] 4.3 Add "New FedEx Order" button (FarmAdmin only) that navigates to the order creation form with `customerId` and `channel=FedEx` pre-populated
- [x] 4.4 Add "View all orders →" link that navigates to `/orders?customerId={id}&channel=FedEx`

## 5. Fulfillment Board — `/orders` Enhancements

- [ ] 5.1 Add a This Week / All toggle control to `Orders/List.razor`; default to "This Week" (Monday of current week)
- [x] 5.1 Add a This Week / All toggle control to `Orders/List.razor`; default to "This Week" (Monday of current week)
- [x] 5.2 Implement week navigation (Prev / Next buttons + date picker) that snaps to Monday — visible only in This Week mode; reuse `SnapToMonday` logic already in `Admin/Orders.razor`
- [x] 5.3 Add week-scoped filtering: when in This Week mode, constrain the displayed orders to `WeekOf` matching the selected week
- [x] 5.4 Move Generate Insulated Orders UI from `Admin/Orders.razor` into `Orders/List.razor` — role-gate to FarmAdmin; show only in This Week mode
- [x] 5.5 Wire the Generate button to the existing `OrderApiClient.GenerateOrdersAsync` method; display the generation summary result
- [x] 5.6 Read `customerId` and `channel` query parameters from the URL on load and pre-populate the corresponding filters; switch to All Orders view when these params are present

## 6. Navigation & Cleanup

- [x] 6.1 Add a redirect from `/standing-orders` to `/orders` in `Routes.razor` or via a thin redirect component
- [x] 6.2 Update `NavMenu.razor`: remove the Standing Orders nav item; ensure Orders links to `/orders`
- [x] 6.3 Remove `Components/Pages/StandingOrders/List.razor`
- [x] 6.4 Remove `Components/Pages/StandingOrders/Detail.razor` (verify all features are now covered by Customer Detail)
- [x] 6.5 Remove `Components/Pages/Admin/Orders.razor` (verify Generate and week browse are fully covered by updated `/orders`)
- [x] 6.6 Verify no remaining internal `NavigationManager` calls reference `/standing-orders` or `/admin/orders`; update any found

## 7. Integration Tests & Final Verification

- [x] 7.1 Add integration test: Customer Detail loads and displays Insulated Schedule card when SO exists
- [x] 7.2 Add integration test: Customer Detail loads and displays FedEx Orders card for a FedEx-channel customer
- [x] 7.3 Add integration test: `/orders` in This Week mode shows only current week's orders by default
- [x] 7.4 Add integration test: Generate button triggers generation and list refreshes in week mode
- [x] 7.5 Add integration test: `/standing-orders` redirects to `/orders`
- [x] 7.6 Run full test suite; confirm all tests pass and no regressions
