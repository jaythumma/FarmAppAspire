## Why

Address forms currently use a free-text input for the US State field, allowing invalid or inconsistently formatted values (e.g. "illinois", "IL.", "Ill"). Replacing it with a dropdown constrained to the 50 US states improves data quality and usability at the point of entry — and this change is timely now that address data drives shipping and billing flows.

## What Changes

- The `State` text input in the customer address form (`CreateEdit.razor`) is replaced with a `<select>` dropdown listing all 50 US state abbreviations (e.g. "IL — Illinois").
- The billing address section in `CreateEdit.razor` receives the same dropdown treatment when the billing fields are visible.
- A shared Blazor component `UsStateSelect.razor` is introduced to avoid duplicating the 50-state list across shipping and billing sections.
- The `AddressSubModel.State` property retains its `[Required, MinLength(1)]` validation — the dropdown enforces a valid selection automatically.
- No API, entity, migration, or DTO changes are required — `State` remains a `string` and existing stored values are unaffected.
- Existing unit and integration tests are unaffected (they pass state values programmatically).
- One UI test scenario is added to verify the dropdown renders with state options.

## Capabilities

### New Capabilities

*(none — this is a UI-only refinement of an existing form field)*

### Modified Capabilities

- `customer-ui`: The State field in address sections changes from free-text input to a constrained 50-state dropdown. Requirements for the create/edit form change to specify that `State` must be selected from a fixed list of valid US state abbreviations.

## Impact

- **`FarmAppAspire.Web/Components/Pages/Customers/CreateEdit.razor`** — shipping and billing `State` inputs replaced by `<UsStateSelect>`.
- **`FarmAppAspire.Web/Components/Shared/UsStateSelect.razor`** (new) — reusable dropdown component containing all 50 US state abbreviations and full names.
- **`FarmAppAspire.Tests.UI/Customers/CustomerFlowTests.cs`** — existing `FillShippingAddressAsync` helper updated to select from the dropdown rather than fill a text input; one new scenario added.
- No changes to `FarmAppAspire.CustomerService`, `FarmAppAspire.ApiService`, `FarmAppAspire.AppHost`, or `FarmAppAspire.ServiceDefaults`.
