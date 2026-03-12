## Context

The customer module (`FarmAppAspire.CustomerService`) accepts customer records with no shipping address and no conditional field validation. `CompanyName` is nullable on the `Customer` entity with no enforcement at any layer. The address sub-resource is entirely free-form — a customer can have zero addresses, multiple billing addresses, and no shipping address.

The `CustomerAddress` entity has a `Type` enum (`Billing | Shipping | Both`) and a boolean `IsDefault` flag. The `Customer` entity has no awareness of billing/shipping distinction at the customer level. The Blazor frontend (`CreateEdit.razor`) does not include address fields on the create/edit form at all.

## Goals / Non-Goals

**Goals:**
- Require a shipping address at customer creation (enforced in the API, not the DB schema).
- Require `CompanyName` when `CustomerType == Wholesale` (enforced in the API on both create and update).
- Add a `BillingUsesShipping` flag to `Customer` that tracks whether billing is derived from shipping or is a separate record.
- When a Shipping address is updated via `PUT /customers/{id}/addresses/{aid}`, automatically clear `BillingUsesShipping` if it was `true`.
- Expose `BillingUsesShipping` in `CustomerDetailDto` so the UI can reflect each of the three billing states.
- Update the Blazor create/edit form and detail page to reflect all new rules.
- Update and add unit, integration, and UI tests for full coverage of new behaviour.

**Non-Goals:**
- Database-level constraints enforcing address cardinality or `CompanyName` nullability — this may be added later.
- Enforcing that a billing address record exists when `BillingUsesShipping = false` — billing can legitimately be "not set".
- Any changes to contacts, payment terms, or the order module.
- Migrating existing customer records to add shipping addresses retroactively.

## Decisions

### D1 — Shipping address embedded in `CreateCustomerRequest` (Option A)
**Decision:** Extend `CreateCustomerRequest` with a required `ShippingAddress: AddressFields` property and a `BillingUsesShipping: bool` (default `true`), plus an optional `BillingAddress: AddressFields?`.

**Rationale:** Requires the shipping address and the customer to be created atomically in one API call. Prevents the existence of a customer in a "incomplete" state between two API calls. All current callers are the Blazor UI (being updated) and the integration test suite (being updated), so the breaking change is fully contained.

**Alternative considered:** Two-step — create customer, then require a Shipping address before activation. Rejected: adds state complexity ("complete" vs "incomplete" customer), requires a new status field, and complicates every downstream consumer that needs to check status.

### D2 — `AddressFields` as a shared embedded record
**Decision:** Introduce a new `AddressFields` record (`Line1`, `Line2?`, `City`, `State`, `PostalCode`, `Country`) for embedded address input in `CreateCustomerRequest`. The `Label` defaults to `"Shipping"` / `"Billing"` at the service layer; `Type` is implied by context.

**Rationale:** Avoids forcing callers to set `Type` and `Label` on an embedded address where those values are always known from context. Keeps `CreateAddressRequest` (which is used for the standalone `POST /customers/{id}/addresses` endpoint) unchanged.

### D3 — `BillingUsesShipping` as a flag on `Customer`, not a derived property
**Decision:** `Customer.BillingUsesShipping: bool` stored in the database. Not computed by comparing address records.

**Rationale:** Computing equality between two address records is brittle — a customer could coincidentally have identical shipping and billing addresses without intending "same as shipping". The stored flag is the explicit intent of the user at save time. Requires a new EF Core migration (no DB constraint, nullable-safe default of `false` for existing rows).

### D4 — Shipping address update clears `BillingUsesShipping`
**Decision:** In the `PUT /customers/{id}/addresses/{aid}` handler, after saving the address update, if `address.Type` is `Shipping` or `Both` AND `customer.BillingUsesShipping == true`, set `customer.BillingUsesShipping = false` and save.

**Rationale:** The user explicitly changed shipping after declaring billing = shipping. The intent is now ambiguous. Forcing the flag to `false` requires a conscious admin action to restore it — either via the "Use shipping address" link or by re-checking the toggle. This is safer than silently keeping billing derived from a changed shipping address.

### D5 — `CompanyName` required for Wholesale at the API layer
**Decision:** Custom validation in both `POST /customers` and `PUT /customers/{id}` handlers — `if (req.Type == Wholesale && string.IsNullOrWhiteSpace(req.CompanyName)) return Results.Problem(...)`. Also enforced in the Blazor `CustomerFormModel` via a conditional `[Required]` using a custom attribute or `IValidatableObject`.

**Rationale:** Data annotations don't support cross-property conditional validation natively. Using `IValidatableObject` on the form model and explicit guard code in the minimal API keeps the logic simple and testable.

### D6 — Billing address display states (three states, not two)
**Decision:** The detail page renders one of three billing address states:
1. `BillingUsesShipping = true` → show "Same as shipping address" badge
2. `BillingUsesShipping = false` AND a Billing address record exists → show the billing address card
3. `BillingUsesShipping = false` AND no Billing address record → show "Not set — [Add billing address] [Use shipping address]"

**Rationale:** State 3 is an explicit "undefined" that guides the admin to resolve it, rather than silently falling back to shipping. This prevents incorrect billing on orders.

## Risks / Trade-offs

- **Existing integration tests break** → Mitigation: `ValidCustomer()` helper in `CustomerEndpointTests` updated to include a `ShippingAddress`; all failing tests updated before new tests are added.
- **Existing customers in production have no `BillingUsesShipping` column** → Mitigation: EF Core migration adds the column with `defaultValue: false`; existing customers will show billing as "not set" until an admin sets it.
- **The flag can get out of sync if addresses are batch-imported** → Mitigation: Accepted for now; no batch import path exists yet. Will be addressed if an import feature is added.
- **`AddressType.Both` enum value becomes ambiguous** → Mitigation: `Both` is preserved in the enum and existing records; the new side-effect logic treats `Both` the same as `Shipping` for the `BillingUsesShipping` flag flip.

## Migration Plan

1. Add `BillingUsesShipping bool` column to `Customers` table (EF Core migration, `defaultValue: false`).
2. Update `CreateCustomerRequest` and `CustomerDetailDto`.
3. Update `POST /customers` handler to create the shipping (and optionally billing) address atomically.
4. Update `PUT /customers/{id}/addresses/{aid}` handler with `BillingUsesShipping` side-effect.
5. Update `CustomerApiClient` in the Web project.
6. Update `CreateEdit.razor` and `Detail.razor`.
7. Update existing tests; add new tests.
8. Rollback: revert migration, restore old request/response shapes, redeploy.

## Open Questions

*(none — all decisions resolved during design exploration)*
