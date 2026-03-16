## Why

The codebase uses two overlapping terms — "Order" and "OrderInstance" — to describe the same concept: an actual delivery for a specific week. This causes confusion at every layer (UI, API routes, entity names, tests). Additionally, FedEx orders are created without any link to a `StandingOrder`, breaking the design consistency and losing the ability to track repeat FedEx customers. The `StandingOrder` entity lacks a `Channel` field, making it implicitly Insulated-only despite FedEx orders logically following the same customer-relationship pattern. Auto-creating a `StandingOrder` inside invoice code further obscures intent.

## What Changes

- **BREAKING** Rename `OrderInstance` → `Order` and `OrderInstanceLine` → `OrderLine` at all layers: EF entity, DB table (`OrderInstances` → `Orders`), DTOs, enums, endpoints, API client, and UI.
- **BREAKING** Make `Order.StandingOrderId` non-nullable — every `Order` must be linked to a `StandingOrder`.
- Add `Channel` (`OrderChannel`) to `StandingOrder` so Insulated and FedEx standing orders are explicitly distinguished.
- Add a unique index on `(CustomerId, Channel)` to `StandingOrders` — one active standing order per customer per channel.
- Replace the separate `POST /customers/{id}/standing-orders` (insulated create) and `POST /customers/{id}/fedex-orders` (FedEx create) flows with a single unified `POST /customers/{id}/orders` endpoint that auto-creates a `StandingOrder` when one does not yet exist for that `(CustomerId, Channel)` combination.
- Remove auto-create-standing-order logic from `CreateInvoiceAsync`; it has no place in the invoice flow.
- Admin bulk generation (`POST /admin/generate-orders`, was `generate-instances`) filters exclusively on `Channel = Insulated` standing orders — FedEx orders are always on-demand.
- Rename all admin routes: `/admin/instances` → `/admin/orders`, `/admin/generate-instances` → `/admin/generate-orders`.
- Merge `FedExOrderEndpoints` into the unified `OrderEndpoints`.
- Provide a multi-step EF migration that backfills `Channel` on existing `StandingOrders` and creates `StandingOrders` for any orphaned FedEx `OrderInstances` before enforcing the NOT NULL constraint.

## Capabilities

### New Capabilities

- `order-model-unification`: Rename `OrderInstance` → `Order` throughout; make `StandingOrderId` required on `Order`; add `Channel` to `StandingOrder`; enforce one standing order per customer per channel; update EF config, DB migration, DTOs, endpoints, and web client to reflect the unified model.
- `order-creation-unification`: Unified order creation endpoint (`POST /customers/{id}/orders`) that handles both Insulated and FedEx channels, auto-creates a `StandingOrder` when none exists, and moves standing-order creation out of the invoice path.

### Modified Capabilities

*(none — no existing spec-level capability files exist; all changes are net-new specifications)*

## Impact

- **`FarmAppAspire.CustomerService/Models/OrderInstance.cs`** — entity renamed to `Order`; `StandingOrderId` becomes required
- **`FarmAppAspire.CustomerService/Models/OrderEnums.cs`** — `OrderInstanceStatus` → `OrderStatus`
- **`FarmAppAspire.CustomerService/Models/OrderDtos.cs`** — all `*Instance*` DTOs renamed; new `Channel` fields on standing order DTOs
- **`FarmAppAspire.CustomerService/Models/StandingOrder.cs`** — add `Channel` property
- **`FarmAppAspire.CustomerService/Data/CustomerDbContext.cs`** — rename DbSets; update FK config (`SetNull` → `Restrict`); add channel index
- **`FarmAppAspire.CustomerService/Endpoints/OrderInstanceEndpoints.cs`** → `OrderEndpoints.cs` — renamed + rerouted; FedEx creation merged in; auto-create standing order logic added
- **`FarmAppAspire.CustomerService/Endpoints/FedExOrderEndpoints.cs`** — deleted; merged into `OrderEndpoints`
- **`FarmAppAspire.CustomerService/Endpoints/AdminEndpoints.cs`** — routes renamed; generate filters on `Channel = Insulated`; standing order auto-create removed from `CreateInvoiceAsync`
- **`FarmAppAspire.CustomerService/Migrations/`** — new migration with data backfill
- **`FarmAppAspire.Web/OrderApiClient.cs`** — all DTO and URL references updated; `CreateOrderAsync` replaces `CreateFedExOrderAsync`
- **`FarmAppAspire.Web/Components/Pages/Orders/`** — CreateEdit.razor unified; no channel-specific branching
- **`FarmAppAspire.Web/Components/Pages/Admin/Orders.razor`** — updated admin route names
- **`FarmAppAspire.Tests.Unit/`** — all `OrderInstance` references updated; new tests for auto-create SO logic and channel filter in generate
- **`FarmAppAspire.Tests.UI/`** — Playwright test route references updated
- No new NuGet packages; no AppHost infrastructure changes
