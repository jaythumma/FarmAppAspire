## Why

The customer module currently accepts customers with no address and applies no type-conditional validation — a Wholesale customer can be saved without a company name, and any customer can exist indefinitely with no shipping address. This makes downstream operations (invoicing, fulfilment, shipping labels) unreliable. These rules need to be enforced at the API layer now, before order management is built on top.

## What Changes

- **BREAKING** `POST /customers` gains two required fields: `shippingAddress` (embedded address object, always required) and `billingUsesShipping` (bool, defaults to `true`). Callers must supply a shipping address at creation time.
- `CompanyName` is required when `CustomerType == Wholesale` — enforced at the API layer on both create and update.
- A `BillingUsesShipping` boolean flag is added to the `Customer` entity (new DB migration, no DB constraint — validation lives in the API).
- `PUT /customers/{id}/addresses/{aid}` gains a side-effect: if the updated address is of type `Shipping` and the customer's `BillingUsesShipping` flag is `true`, the flag is automatically set to `false` (billing is now undefined).
- `CustomerDetailDto` exposes `BillingUsesShipping` so the frontend can reflect current state.
- The Blazor create/edit form gains a mandatory shipping address section and an optional billing address section with a "Same as shipping address" checkbox.
- The customer detail page gains a dedicated billing address section that shows state: "same as shipping", the billing address record, or "not set" with action links.
- Existing integration tests for `PostCustomer` and address endpoints are updated to satisfy the new required fields.
- New unit, integration, and UI tests cover all new validation rules and the billing flag lifecycle.

## Capabilities

### New Capabilities

- `customer-billing-address`: Tracks whether a customer's billing address is the same as their shipping address via a `BillingUsesShipping` flag. Defines what "billing undefined" means, how the flag flips, and how the UI surfaces each state.

### Modified Capabilities

- `customer-crud`: `CompanyName` is now required for Wholesale customers (create + update). `CreateCustomerRequest` gains required `shippingAddress` and `billingUsesShipping` fields — **breaking change** to the create contract.
- `customer-addresses`: `PUT /customers/{id}/addresses/{aid}` gains a side-effect — updating a Shipping address when `BillingUsesShipping = true` clears the flag.
- `customer-ui`: Create form gains mandatory shipping address section and optional billing section with "same as shipping" toggle. Detail page gains billing address section with three distinct display states.

## Impact

- **`FarmAppAspire.CustomerService`** — `Customer.cs` (new field), `CustomerDtos.cs` (new request/response fields), `Program.cs` (updated POST and PUT handlers, new validation), new EF Core migration.
- **`FarmAppAspire.Web`** — `Components/Pages/Customers/CreateEdit.razor` (shipping + billing sections, conditional CompanyName required), `Components/Pages/Customers/Detail.razor` (billing address section), `CustomerApiClient.cs` (updated request/response types).
- **`FarmAppAspire.Tests`** — `CustomerEndpointTests.cs` updated for new create contract; new tests for Wholesale validation, shipping required, billing flag lifecycle.
- **`FarmAppAspire.Tests.Unit`** — New tests for `CompanyName` conditional requirement and `BillingUsesShipping` model behaviour.
- **`FarmAppAspire.Tests.UI`** — `CustomerFlowTests.cs` updated; new scenarios for shipping section, billing toggle, CompanyName required.
- No changes to `FarmAppAspire.AppHost`, `FarmAppAspire.ApiService`, or `FarmAppAspire.ServiceDefaults`.
