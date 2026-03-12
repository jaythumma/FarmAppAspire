## Why

The farm sells Curry Leaf to retail and wholesale customers and currently has no system to record, track, or manage orders. All order coordination happens informally (phone/email) with no invoice trail, no recurring order automation, and no visibility into what ships each week. Building the customer-orders capability turns this manual process into a managed, auditable system — starting with the insulated-box standing-order channel (the farm's primary revenue stream) and the FedEx/Amazon channel.

## What Changes

- **New OrderService domain** within `FarmAppAspire.CustomerService` (same microservice): product catalog, pricing, standing orders, order instances, FedEx one-time orders, invoice labeling, customer keys.
- **Product catalog**: CurryLeaf product with two box categories — Insulated (5/10/12 lb, priced by lb) and FedEx (8 oz-tiers, fixed price). Base insulated price $13/lb.
- **Customer pricing**: Admin/Manager can set per-customer override pricing (insulated only) via the management UI. FedEx pricing is fixed and non-negotiable.
- **Standing orders** (insulated channel only): Auto-created on a customer's first invoice. Supports multiple order lines (e.g. `5lb × 3, 12lb × 10`). Frequency options: Weekly, BiWeekly, Monthly (nth Monday), OnRequest, or Stopped. One active standing order per customer now; multi-standing-order support is a future feature.
- **Standing-order schedule**: Friday cutoff deadline for skip requests. Post-cutoff skips are ignored — order ships and invoices. BiWeekly rhythm resumes from the week after a skip. OnRequest = auto-generate instance, fulfill, then auto-pause for subsequent weeks. Staff manually pause/resume.
- **Order instances**: Auto-generated each season week from active standing orders. Harvest calendar window Thu–Mon. USDA inspection gate (Fri and Mon only); failed leaf is discarded and replaced with freshly harvested, passed leaf — orders are always fulfilled. Statuses: Pending → Harvested → Inspected → Shipped | Cancelled.
- **FedEx orders**: Manually entered by staff. One-time (no standing order). 8-tier fixed oz pricing. Envelopes for < 2 lb, boxes for ≥ 2 lb. AMZ channel prefix on invoices.
- **Invoice labeling**: Insulated — `{CustomerKey}-{SeasonYear}-{SeekNum}`. FedEx — `AMZ-{CustomerKey}-{SeasonYear}-{SeekNum}`. SeekNum is sequential per customer, per season, per channel. Sample orders are never invoiced.
- **Customer key**: Short unique identifier derived from display-name abbreviation + city/state abbreviation. Used in invoice labels and reporting.
- **Order contacts**: Any contact at a wholesale customer may place orders. Retail orders carry no contact. Future: authorized-contact restriction.
- **Season year**: First Monday of June → last Monday of May (following year).

## Capabilities

### New Capabilities

- `product-catalog`: CurryLeaf product; insulated box sizes (5/10/12 lb, $13/lb base); FedEx oz-tier sizes (8 tiers, fixed pricing); box category enum; packaging threshold (envelope < 2 lb, box ≥ 2 lb)
- `customer-pricing`: Per-customer override pricing for insulated boxes; Admin/Manager sets via management UI; price resolution logic (override → $13/lb default); FedEx pricing always fixed
- `standing-order-setup`: Standing order data model; multiple lines with aggregated totals; IsSample flag and sample→live transition; auto-create on first invoice (weekly default); season year lifecycle; one active per customer
- `standing-order-schedule`: All frequency options (Weekly/BiWeekly/Monthly/OnRequest/Stopped); Friday cutoff enforcement; skip week mechanics; BiWeekly disruption/resume behavior; OnRequest generate-fulfill-pause pattern; staff pause/resume
- `order-instance-lifecycle`: Weekly instance generation; harvest calendar (Thu–Mon); USDA inspection gate (Fri + Mon only); fail → discard → fulfill with fresh leaf; instance status progression; Friday vs Monday ship decision
- `fedex-orders`: Manual staff entry; one-time non-standing orders; 8-tier fixed oz pricing; envelope vs box threshold; AMZ invoice prefix; separate from insulated channel
- `invoice-labeling`: Label format; SeasonYear (June-start); SeekNum sequential per customer per season per channel; IsSample = no invoice; invoice triggered at Shipped status
- `customer-key`: CustomerKey construction from display-name abbreviation + city/state abbreviation; uniqueness rules
- `order-contacts`: Contact association on standing orders and instances; any wholesale contact may order; retail requires no contact; future authorized-contact extension point

### Modified Capabilities

*(none — this is a fully additive change)*

## Impact

- **`FarmAppAspire.CustomerService`** — new entities (Product, BoxSizePricing, CustomerPricing, StandingOrder, StandingOrderLine, OrderInstance, OrderInstanceLine, Invoice), new endpoints, new migrations, new EF Core DbSets.
- **`FarmAppAspire.Web`** — new Blazor pages: order management, standing order setup, pricing overrides, FedEx order entry, invoice list.
- **`FarmAppAspire.CustomerService/Data`** — new `OrderDbContext` or extension of `CustomerDbContext`.
- **`FarmAppAspire.Tests`** and **`FarmAppAspire.Tests.Unit`** — new integration and unit tests for all order workflows.
- **New NuGet packages**: possibly `Cronos` or similar for recurring schedule generation.
- **No breaking changes** to existing customer, contact, or address APIs.
