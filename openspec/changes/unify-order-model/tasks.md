## 1. EF Migration — Backfill and Schema Changes

- [ ] 1.1 Add migration: add nullable `Channel` (string) column to `StandingOrders` table
- [ ] 1.2 Add migration SQL: `UPDATE StandingOrders SET Channel = 'Insulated' WHERE Channel IS NULL`
- [ ] 1.3 Add migration SQL: for each `OrderInstance` where `StandingOrderId IS NULL`, group by `(CustomerId, Channel)`, insert one `StandingOrder` per group (`Channel`, `Frequency = 'OnRequest'`, `StartWeek = MIN(WeekOf)`, `Status = 'Active'`, `SeasonYear`, `CreatedAt`, `CreatedBy`)
- [ ] 1.4 Add migration SQL: `UPDATE OrderInstances SET StandingOrderId = <new SO id>` for the backfilled records
- [ ] 1.5 Add migration: alter `StandingOrders.Channel` to NOT NULL
- [ ] 1.6 Add migration: alter `OrderInstances.StandingOrderId` to NOT NULL
- [ ] 1.7 Add migration: drop existing FK with `SetNull` behavior; add FK with `Restrict` behavior
- [ ] 1.8 Add migration: add unique index `IX_StandingOrders_CustomerId_Channel` on `(CustomerId, Channel)`
- [ ] 1.9 Add migration: rename table `OrderInstances` → `Orders`
- [ ] 1.10 Add migration: rename table `OrderInstanceLines` → `OrderLines`

## 2. Domain Models — Rename and Extend

- [ ] 2.1 Rename `OrderInstance.cs` → `Order.cs`; rename class `OrderInstance` → `Order`, navigation `Instances` on `StandingOrder` → `Orders`
- [ ] 2.2 Rename class `OrderInstanceLine` → `OrderLine`; update all FK property names (`OrderInstanceId` → `OrderId`, `OrderInstance` nav → `Order`)
- [ ] 2.3 In `OrderEnums.cs`: rename `OrderInstanceStatus` → `OrderStatus`
- [ ] 2.4 In `StandingOrder.cs`: add `Channel` property (`OrderChannel`); update `ICollection<OrderInstance> Instances` → `ICollection<Order> Orders`
- [ ] 2.5 Change `Order.StandingOrderId` from `Guid?` to `Guid`; change `Order.StandingOrder` navigation from nullable to required

## 3. DTOs — Rename

- [ ] 3.1 In `OrderDtos.cs`: rename `OrderInstanceLineDto` → `OrderLineDto`
- [ ] 3.2 Rename `OrderInstanceDto` → `OrderDto`; update all record constructor field references
- [ ] 3.3 Rename `UpdateOrderInstanceRequest` → `UpdateOrderRequest`
- [ ] 3.4 Rename `GenerateInstancesRequest` → `GenerateOrdersRequest`; rename `GenerateInstancesResponse` → `GenerateOrdersResponse`
- [ ] 3.5 Rename `GeneratedInstanceSummary` → `GeneratedOrderSummary`; rename `WeekInstanceSummary` → `WeekOrderSummary`
- [ ] 3.6 In `StandingOrderDto` and `AllStandingOrderDto` records: add `Channel` field (`OrderChannel`)
- [ ] 3.7 In `CreateStandingOrderRequest`: add `Channel` field (`OrderChannel`) — used only for explicit SO creation paths

## 4. EF DbContext — Update Configuration

- [ ] 4.1 Rename `DbSet<OrderInstance> OrderInstances` → `DbSet<Order> Orders`
- [ ] 4.2 Rename `DbSet<OrderInstanceLine> OrderInstanceLines` → `DbSet<OrderLine> OrderLines`
- [ ] 4.3 Update `modelBuilder.Entity<Order>()` config: rename all type/property references; change `OnDelete(SetNull)` → `OnDelete(Restrict)` on the `StandingOrder → Orders` relationship
- [ ] 4.4 Update `modelBuilder.Entity<OrderLine>()` config: rename FK property `OrderInstanceId` → `OrderId`
- [ ] 4.5 Update `modelBuilder.Entity<StandingOrder>()` config: add `e.Property(x => x.Channel).HasConversion<string>()`; add unique index `e.HasIndex(x => new { x.CustomerId, x.Channel }).IsUnique()`
- [ ] 4.6 Update `Invoice` entity config to reference `Order` instead of `OrderInstance` for the FK

## 5. OrderEndpoints — Rename, Merge, and Unify

- [ ] 5.1 Rename `OrderInstanceEndpoints.cs` → `OrderEndpoints.cs`; rename class; rename extension method to `MapOrderEndpoints()`
- [ ] 5.2 Update all internal references from `OrderInstance`/`OrderInstanceLine`/`OrderInstanceDto` to `Order`/`OrderLine`/`OrderDto` within the file
- [ ] 5.3 Change route prefix `customers/{customerId:guid}/order-instances` → `customers/{customerId:guid}/orders`
- [ ] 5.4 Change global route `GET /order-instances` → `GET /orders` (all-orders list endpoint)
- [ ] 5.5 Merge `FedExOrderEndpoints.MapFedExOrderEndpoints()` GET endpoints into `OrderEndpoints` with `Channel = FedEx` filter (replacing `/fedex-orders` routes with `/orders?channel=FedEx`)
- [ ] 5.6 Add unified `POST /customers/{customerId:guid}/orders` endpoint: validate customer + contact; resolve or auto-create `StandingOrder` by `(CustomerId, Channel)`; check idempotency (existing order for same SO + WeekOf → HTTP 409); create `Order`; return HTTP 201
- [ ] 5.7 Remove `CreateInvoiceAsync` standing-order auto-create block; assert `StandingOrderId` is already set
- [ ] 5.8 Update `ToDto` helper to use `OrderDto`, `OrderLineDto`; include `StandingOrderId` (now guaranteed non-null)
- [ ] 5.9 Delete `FedExOrderEndpoints.cs`

## 6. AdminEndpoints — Rename Routes and Add Channel Filter

- [ ] 6.1 Change `POST /admin/generate-instances` → `POST /admin/generate-orders`; update request/response DTO type names
- [ ] 6.2 Add `.Where(s => s.Channel == OrderChannel.Insulated)` to the standing-order query in the generate endpoint
- [ ] 6.3 Change `GET /admin/instances` → `GET /admin/orders`; update DTO type names
- [ ] 6.4 Add XML doc comment on the generate endpoint explaining the `Channel = Insulated` filter is intentional

## 7. StandingOrderEndpoints — Add Channel to DTOs and Creation

- [ ] 7.1 Update `ToDto` helper to populate `Channel` in `StandingOrderDto` and `AllStandingOrderDto`
- [ ] 7.2 Update `GET /standing-orders` and `GET /customers/{id}/standing-orders` to include `Channel` in returned DTOs
- [ ] 7.3 Remove `POST /customers/{id}/standing-orders` from the public API (or gate it to admin-only if a standalone SO creation path is needed internally)

## 8. Program.cs — Update Registrations

- [ ] 8.1 Replace `app.MapOrderInstanceEndpoints()` with `app.MapOrderEndpoints()`
- [ ] 8.2 Remove `app.MapFedExOrderEndpoints()` (deleted in step 5.9)

## 9. Web Client — OrderApiClient.cs

- [ ] 9.1 Update `GetOrdersAsync` URL from `/customers/{id}/order-instances` → `/customers/{id}/orders`
- [ ] 9.2 Update `GetAllOrdersAsync` URL from `/order-instances` → `/orders`
- [ ] 9.3 Update `GetOrderAsync` URL from `/customers/{id}/order-instances/{id}` → `/customers/{id}/orders/{id}`
- [ ] 9.4 Replace `CreateFedExOrderAsync` and `CreateStandingOrderAsync` (creation path) with unified `CreateOrderAsync(Guid customerId, CreateOrderRequest request, CancellationToken)` that posts to `/customers/{id}/orders`
- [ ] 9.5 Add `CreateOrderRequest` record to `OrderApiClient.cs`: `(OrderChannel Channel, DateTime WeekOf, IReadOnlyList<OrderLineRequest> Lines, Guid? ContactId, bool IsSample)`
- [ ] 9.6 Add `OrderLineRequest` record: `(OrderChannel Channel, InsulatedBoxSize? BoxSize, FedExTierSize? FedExTierSize, int Qty)` — discriminated by Channel
- [ ] 9.7 Rename web-side record `OrderSummary.StandingOrderId` to be non-nullable `Guid` (was `Guid?`)
- [ ] 9.8 Add `Channel` field to `StandingOrderDetail` and `AllStandingOrderSummary` web records
- [ ] 9.9 Update admin client methods: `BrowseWeekInstancesAsync` → `BrowseWeekOrdersAsync` (URL `/admin/orders`); `GenerateInstancesAsync` → `GenerateOrdersAsync` (URL `/admin/generate-orders`)

## 10. Blazor UI — Update Pages and Components

- [ ] 10.1 In `Orders/CreateEdit.razor`: replace channel-branching logic (`CreateStandingOrderAsync` vs `CreateFedExOrderAsync`) with a single call to `OrderApi.CreateOrderAsync`; update form model to include `Channel` selector
- [ ] 10.2 In `Orders/List.razor`: update any hardcoded "order-instances" route references; verify column labels use "Order" not "Order Instance"
- [ ] 10.3 In `Admin/Orders.razor`: update `_browseAsync` call to use `BrowseWeekOrdersAsync`; update `_generateAsync` call to use `GenerateOrdersAsync`; update any "instance" label copy to "order"
- [ ] 10.4 In `StandingOrders/List.razor` and `StandingOrders/Detail.razor`: display `Channel` field in the standing order view
- [ ] 10.5 Search all `.razor` files for "instance" (case-insensitive) and update user-visible labels

## 11. Unit Tests — Rename and Extend

- [ ] 11.1 Update all unit test files that reference `OrderInstance`, `OrderInstanceLine`, `OrderInstanceStatus` to use the new names
- [ ] 11.2 Update `AdminGenerateInstancesTests.cs`: rename file → `AdminGenerateOrdersTests.cs`; update DTO/enum references; add test asserting FedEx standing orders are excluded from generation
- [ ] 11.3 Add unit test in `AdminGenerateOrdersTests.cs`: `OnRequest` insulated SO is generated once and auto-paused (regression guard)
- [ ] 11.4 Add unit test: `CreateOrderAsync` auto-creates a `StandingOrder` when none exists for `(CustomerId, Channel)` — Insulated path
- [ ] 11.5 Add unit test: `CreateOrderAsync` auto-creates a FedEx `StandingOrder` with `Frequency = OnRequest` and empty lines
- [ ] 11.6 Add unit test: `CreateOrderAsync` reuses existing `StandingOrder` on subsequent call for same `(CustomerId, Channel)`
- [ ] 11.7 Add unit test: duplicate order (same `StandingOrderId` + `WeekOf`) returns HTTP 409
- [ ] 11.8 Add unit test: `CreateInvoiceAsync` does NOT create a `StandingOrder` (assert no new SO inserted during ship/invoice flow)
- [ ] 11.9 Update `AdminOrdersErrorHandlingTests.cs`: route references `/admin/orders` not `/admin/instances`

## 12. UI Tests — Update Route References

- [ ] 12.1 In `OrderManagementUITests.cs`: update any hardcoded `/order-instances`, `/admin/instances`, or `/fedex-orders` URL segments to the new routes
- [ ] 12.2 In `StandingOrdersUITests.cs` and `InvoicesUITests.cs`: verify no broken route references
- [ ] 12.3 Add Playwright test: navigate to `/admin/orders`, confirm page returns HTTP 200 and renders weekly order table

## 13. Verification

- [ ] 13.1 Run `dotnet build` — confirm zero errors
- [ ] 13.2 Run `dotnet test FarmAppAspire.Tests.Unit` — confirm all unit tests pass
- [ ] 13.3 Run `dotnet run --project FarmAppAspire.AppHost` — confirm app starts and `/admin/orders` loads without HTTP 500
- [ ] 13.4 Verify via browser: "Add Order" creates an order for both Insulated and FedEx channels; verify a `StandingOrder` is auto-created for a new customer+channel combination
- [ ] 13.5 Verify via browser: Standing Orders list shows `Channel` column for each standing order
