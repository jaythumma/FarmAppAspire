## 1. Data Model & Migrations

- [ ] 1.1 Add `CustomerKey` (string, nullable) column to `Customer` entity and migration
- [ ] 1.2 Create `InsulatedBoxConfig` entity (BoxSize enum, WeightLbs, IsDefault) + EF seed data (5lb, 10lb, 12lb; default = 12lb)
- [ ] 1.3 Create `FedExTierConfig` entity (TierSize enum, WeightLbs, FixedPrice) + EF seed data (8 tiers)
- [ ] 1.4 Create `CustomerPricing` entity (Id, CustomerId, BoxSize, PricePerLb, ShippingRate, MinQty) + migration
- [ ] 1.5 Create `StandingOrder` entity (Id, CustomerId, ContactId, Status, Frequency, MonthlyWeek, IsSample, SeasonYear, StartWeek) + migration
- [ ] 1.6 Create `StandingOrderLine` entity (Id, StandingOrderId, BoxSize, Qty) + migration
- [ ] 1.7 Create `StandingOrderSkip` entity (Id, StandingOrderId, WeekOf) + migration
- [ ] 1.8 Create `OrderInstance` entity (Id, StandingOrderId nullable, CustomerId, ContactId, Channel, WeekOf, Status, ShipDate nullable, IsSample) + migration
- [ ] 1.9 Create `OrderInstanceLine` entity (Id, OrderInstanceId, BoxSize, Qty, EffectivePricePerLb) + migration
- [ ] 1.10 Create `Invoice` entity (Id, OrderInstanceId, CustomerId, Channel, SeasonYear, SeekNum, Label, CreatedAt) + migration
- [ ] 1.11 Add DB indexes: `OrderInstance(CustomerId, WeekOf, Status)`, `Invoice(CustomerId, SeasonYear, Channel, SeekNum)`, `StandingOrder(CustomerId, Status)`
- [ ] 1.12 Register all new entities in `CustomerDbContext`; run `dotnet ef migrations add OrderManagement`

## 2. Domain Enums & Value Types

- [ ] 2.1 Add `BoxCategory` enum: `Insulated | FedEx`
- [ ] 2.2 Add `InsulatedBoxSize` enum: `FiveLb | TenLb | TwelveLb`
- [ ] 2.3 Add `FedExTierSize` enum: `OneOz | TwoOz | FourOz | EightOz | OneLb | TwoLb | ThreeLb | FiveLb`
- [ ] 2.4 Add `OrderFrequency` enum: `Weekly | BiWeekly | Monthly | OnRequest | Stopped`
- [ ] 2.5 Add `MonthlyWeek` enum: `First | Second | Third | Fourth`
- [ ] 2.6 Add `StandingOrderStatus` enum: `Active | Paused | Stopped`
- [ ] 2.7 Add `OrderInstanceStatus` enum: `Pending | Harvested | Inspected | Shipped | Cancelled`
- [ ] 2.8 Add `OrderChannel` enum: `Insulated | FedEx`
- [ ] 2.9 Add `FedExPackagingType` enum: `Envelope | Box`

## 3. Product Catalog Service

- [ ] 3.1 Implement `GET /products/catalog` — returns insulated box configs and FedEx tier configs
- [ ] 3.2 Implement `GET /products/insulated-boxes` — returns all insulated box sizes with base pricing
- [ ] 3.3 Implement `GET /products/fedex-tiers` — returns all FedEx tier sizes with fixed pricing
- [ ] 3.4 Unit tests: price computation for insulated (N × size × $13/lb) and FedEx tier lookup

## 4. Customer Pricing Endpoints

- [ ] 4.1 Implement `GET /customers/{id}/pricing` — returns all customer pricing overrides
- [ ] 4.2 Implement `POST /customers/{id}/pricing` — create override (FarmAdmin/Manager only); validate BoxCategory = Insulated
- [ ] 4.3 Implement `PUT /customers/{id}/pricing/{priceId}` — update override
- [ ] 4.4 Implement `DELETE /customers/{id}/pricing/{priceId}` — remove override
- [ ] 4.5 Implement price resolution service: given (CustomerId, BoxSize, Qty) → returns EffectivePricePerLb
- [ ] 4.6 Unit tests: override priority, MinQty threshold, FedEx always returns fixed price

## 5. Customer Key Service

- [ ] 5.1 Implement `CustomerKeyService.Generate(customer)` — deterministic algorithm (first 3 consonant-filtered chars per significant word; ignore LLC/Inc/Co/The; max 6 chars + City abbr + State)
- [ ] 5.2 Implement `CustomerKeyService.CheckCollision(proposedKey, customerId)` — returns true if another customer already has that key
- [ ] 5.3 Implement `POST /customers/{id}/key/generate` — generate + store key; return HTTP 409 if collision
- [ ] 5.4 Implement `PUT /customers/{id}/key` — staff manually sets key; validates uniqueness
- [ ] 5.5 Unit tests: abbreviation algorithm, collision detection, key stability on rename

## 6. Standing Order Endpoints

- [ ] 6.1 Implement `GET /customers/{id}/standing-orders` — list standing orders with aggregated totals
- [ ] 6.2 Implement `GET /customers/{id}/standing-orders/{soId}` — detail with lines and skip schedule
- [ ] 6.3 Implement `POST /customers/{id}/standing-orders` — create; validate single-active rule (HTTP 409 if duplicate); validate ContactId belongs to customer
- [ ] 6.4 Implement `PUT /customers/{id}/standing-orders/{soId}` — update frequency, MonthlyWeek, IsSample, ContactId; validate status transitions
- [ ] 6.5 Implement `POST /customers/{id}/standing-orders/{soId}/pause` — set Status=Paused
- [ ] 6.6 Implement `POST /customers/{id}/standing-orders/{soId}/resume` — set Status=Active
- [ ] 6.7 Implement `POST /customers/{id}/standing-orders/{soId}/stop` — set Frequency=Stopped; permanent
- [ ] 6.8 Implement `POST /customers/{id}/standing-orders/{soId}/skip` — add skip week; validate Friday cutoff; HTTP 422 if after cutoff
- [ ] 6.9 Implement `DELETE /customers/{id}/standing-orders/{soId}/skip/{weekOf}` — remove skip (must be before cutoff)
- [ ] 6.10 Integration tests: all frequency types, skip cutoff, BiWeekly resume logic, OnRequest auto-pause, sample→live transition

## 7. Order Instance Generation Service

- [ ] 7.1 Implement `StandingOrderGenerationService : IHostedService` — runs on configurable schedule; generates instances for all Active standing orders for the upcoming Monday
- [ ] 7.2 Implement idempotency check: skip if instance already exists for (StandingOrderId, WeekOf)
- [ ] 7.3 Implement frequency logic: Weekly (always), BiWeekly (alternate from StartWeek, adjusted for skips), Monthly (nth Monday), OnRequest (generate + immediately set SO back to Paused)
- [ ] 7.4 Implement skip exclusion: skip generation if WeekOf is in StandingOrderSkip for this SO
- [ ] 7.5 Season boundary: stop generating if WeekOf is outside current season (first Monday June → last Monday May)
- [ ] 7.6 Implement `POST /admin/generate-instances` manual trigger endpoint (FarmAdmin only)
- [ ] 7.7 Unit tests: BiWeekly logic, Monthly nth-Monday calculation, OnRequest auto-pause, season boundary

## 8. Order Instance Lifecycle Endpoints

- [ ] 8.1 Implement `GET /customers/{id}/order-instances` — list instances with status filter; paginated
- [ ] 8.2 Implement `GET /customers/{id}/order-instances/{instanceId}` — detail
- [ ] 8.3 Implement `POST /customers/{id}/order-instances/{instanceId}/harvest` — Pending → Harvested
- [ ] 8.4 Implement `POST /customers/{id}/order-instances/{instanceId}/inspect` — Harvested → Inspected; validate day = Friday or Monday (HTTP 422 otherwise)
- [ ] 8.5 Implement `POST /customers/{id}/order-instances/{instanceId}/ship` — Inspected → Shipped; record ShipDate; trigger invoice creation if IsSample=false
- [ ] 8.6 Implement `POST /customers/{id}/order-instances/{instanceId}/cancel` — Pending → Cancelled only; validates Friday cutoff for standing-order instances
- [ ] 8.7 Integration tests: full lifecycle happy path, invalid transitions, USDA day validation, sample suppresses invoice

## 9. FedEx Order Endpoints

- [ ] 9.1 Implement `POST /customers/{id}/fedex-orders` — create one-time FedEx instance; validate tier size; set price from FedExTierConfig; derive PackagingType from total weight; ContactId optional
- [ ] 9.2 Implement `GET /customers/{id}/fedex-orders` — list FedEx instances with status
- [ ] 9.3 Implement `GET /customers/{id}/fedex-orders/{instanceId}` — detail
- [ ] 9.4 FedEx instances reuse lifecycle endpoints from Group 8 (harvest, inspect, ship, cancel)
- [ ] 9.5 Integration tests: tier price enforcement, packaging threshold (1.5 lb → Envelope, 2 lb → Box), AMZ invoice label

## 10. Invoice Service & Endpoints

- [ ] 10.1 Implement `InvoiceService.CreateInvoice(instanceId)` — atomic SeekNum assignment using pessimistic lock on `(CustomerId, SeasonYear, Channel)`; build label string; persist Invoice
- [ ] 10.2 Implement `SeasonYearService.CurrentSeasonYear(date)` — returns year of first Monday of June ≤ date
- [ ] 10.3 Implement `GET /customers/{id}/invoices` — list invoices with label, amount, date; filterable by season year and channel
- [ ] 10.4 Implement `GET /customers/{id}/invoices/{invoiceId}` — invoice detail with instance lines
- [ ] 10.5 Unit tests: SeasonYear derivation (edge cases: first/last week of season), SeekNum atomicity, label format for both channels, IsSample = no invoice

## 11. Season Management

- [ ] 11.1 Implement `GET /admin/season/current` — returns current season year and week boundaries
- [ ] 11.2 Implement `POST /admin/season/restart` — FarmAdmin only; creates new season-year record; SeekNum counters reset to 0 for new season; generation service uses new season year going forward
- [ ] 11.3 Unit tests: season boundary dates (June 1 vs last Monday of May edge cases)

## 12. Blazor Management UI

- [ ] 12.1 Customer Pricing page: list, add, edit, delete per-customer insulated pricing overrides (Admin/Manager only)
- [ ] 12.2 Standing Order setup page: view/edit standing order (frequency, lines, IsSample, ContactId, pause/resume/stop)
- [ ] 12.3 Skip Week management: add/remove skip dates with cutoff enforcement feedback
- [ ] 12.4 Order Instance list page: filter by status, week range; bulk status actions (mark harvested, mark inspected, mark shipped)
- [ ] 12.5 FedEx order entry page: select customer, tier sizes, quantities; submit one-time order
- [ ] 12.6 Invoice list page: filterable by customer, season year, channel; show label and amount
- [ ] 12.7 Customer Key management: generate, view, manual override UI
- [ ] 12.8 Admin: manual instance generation trigger and season restart

## 13. Unit Tests (FarmAppAspire.Tests.Unit)

- [ ] 13.1 CustomerKeyService abbreviation algorithm (20+ cases covering LLC, multi-word, collision)
- [ ] 13.2 PriceResolutionService (override vs default, MinQty threshold, FedEx always fixed)
- [ ] 13.3 SeasonYearService (date-to-season-year for all months; year boundary edge cases)
- [ ] 13.4 BiWeekly generation logic (skip disruption, resume from correct week)
- [ ] 13.5 Monthly nth-Monday calculation (all four ordinals, month boundaries)
- [ ] 13.6 InvoiceService label construction (insulated and AMZ formats)
- [ ] 13.7 StandingOrder aggregated totals (multi-line Σ)

## 14. Integration Tests (FarmAppAspire.Tests)

- [ ] 14.1 Full standing order lifecycle: create → generate → harvest → inspect → ship → invoice
- [ ] 14.2 Skip week: valid (before cutoff) and invalid (after cutoff) paths
- [ ] 14.3 OnRequest pattern: generate + ship + auto-pause; release again
- [ ] 14.4 Sample order: shipped but no invoice; IsSample=false transition; next instance invoiced
- [ ] 14.5 FedEx order: create, lifecycle, AMZ invoice label, independent SeekNum counter
- [ ] 14.6 Customer pricing override: resolution priority, MinQty, FedEx rejection
- [ ] 14.7 Single-active standing order constraint (HTTP 409 on second create)
- [ ] 14.8 Season restart: SeekNum resets, new season year on invoices

## 15. Verification

- [ ] 15.1 `dotnet build` — zero errors
- [ ] 15.2 `dotnet test FarmAppAspire.Tests.Unit` — all pass
- [ ] 15.3 `dotnet test FarmAppAspire.Tests` — all pass
- [ ] 15.4 `dotnet test FarmAppAspire.Tests.UI` — all pass (existing tests unaffected)
