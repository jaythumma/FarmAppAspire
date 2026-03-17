## Why

The current UI exposes Standing Orders and Orders as two disconnected screens, forcing both admins and users to navigate between them to understand a customer's delivery picture. FedEx orders are also unnecessarily routed through the StandingOrder machinery they don't need, creating hollow records and hidden side effects.

## What Changes

- **Remove** the `/standing-orders` global list page and `/standing-orders/create` page
- **Remove** the `/admin/orders` page (its Generate functionality merges into `/orders`)
- **Add** Insulated Schedule management card to Customer Detail (subscription config, lines, skips, lifecycle actions)
- **Add** FedEx Orders card to Customer Detail (recent orders, ad-hoc order creation)
- **Modify** `/orders` fulfillment board: add This Week / All toggle, week navigation, and role-gated Generate button
- **BREAKING** `Order.StandingOrderId` becomes nullable — FedEx orders no longer create a StandingOrder
- **Remove** `OrderFrequency.OnRequest` (no longer needed once FedEx is decoupled)
- **Remove** FedEx auto-create StandingOrder logic from `POST /customers/{id}/orders`

## Capabilities

### New Capabilities

- `customer-insulated-schedule`: View and manage an Insulated standing order subscription directly from Customer Detail — includes frequency, box lines, skip weeks, and pause/resume/stop lifecycle
- `customer-fedex-orders`: View recent FedEx orders and create new ad-hoc FedEx orders from Customer Detail
- `orders-fulfillment-board`: Unified `/orders` page with This Week / All toggle, week navigation, role-gated weekly generation, and cross-channel order management in one place

### Modified Capabilities

<!-- No existing specs to delta against — all capabilities are new surfaces -->

## Impact

- **`FarmAppAspire.CustomerService`**: `Order.StandingOrderId` → `Guid?`; DB migration; remove FedEx auto-create SO in `OrderEndpoints`; remove `OrderFrequency.OnRequest`
- **`FarmAppAspire.Web`**: Remove `Components/Pages/StandingOrders/List.razor`, `StandingOrders/Detail.razor` (redirect to customer detail); remove `Components/Pages/Admin/Orders.razor`; expand `Customers/Detail.razor`; expand `Orders/List.razor`
- **`FarmAppAspire.Web/NavMenu.razor`**: Remove Standing Orders nav link; consolidate Admin section
- **`FarmAppAspire.Tests` / `FarmAppAspire.Tests.Unit`**: Update FedEx order creation tests; add tests for new Customer Detail sections
- No changes to StandingOrder entity, schedule generation service, pricing, or any other existing backend logic
