## 1. Create UsStateSelect Component

- [x] 1.1 Create `FarmAppAspire.Web/Components/Shared/UsStateSelect.razor` as a reusable `InputSelect<string>` wrapper
- [x] 1.2 The component must expose `@bind-Value` (use `@inherits InputBase<string>` or explicit `Value` / `ValueChanged` / `ValueExpression` triple — use `InputSelect` composition)
- [x] 1.3 Include a blank placeholder `<option value="">— select state —</option>` as the first option
- [x] 1.4 List all 50 US states as `<option value="AB">Full Name</option>` (abbreviation as value, full name as label), alphabetically by full name
- [x] 1.5 Accept a `class` attribute (HTML attribute splatting via `@attributes`) so callers can pass `class="form-select"`

## 2. Update CreateEdit.razor

- [x] 2.1 Replace the shipping `<InputText @bind-Value="_model.ShippingAddress.State" class="form-control" />` with `<UsStateSelect @bind-Value="_model.ShippingAddress.State" class="form-select" />`
- [x] 2.2 Replace the billing `<InputText @bind-Value="_model.BillingAddress.State" class="form-control" />` with `<UsStateSelect @bind-Value="_model.BillingAddress.State" class="form-select" />`
- [x] 2.3 Update the Shipping Address section label: "State" remains as-is (still required via `IValidatableObject`)
- [x] 2.4 Confirm `<ValidationMessage For="() => _model.ShippingAddress.State" />` is still in place after the replacement

## 3. Update UI Tests

- [x] 3.1 Update `FillShippingAddressAsync` helper in `CustomerFlowTests.cs`: change `await inputs.Nth(3).FillAsync(state)` to use `page.Locator("select:has(option[value='AL'])").First.SelectOptionAsync(state)` for the state dropdown
- [x] 3.2 Add test: `CreateForm_ShippingStateField_IsDropdown` — navigates to create form, waits for circuit, asserts a `<select>` element exists within the Shipping Address section
- [x] 3.3 Add test: `CreateForm_StateDropdown_ContainsAllStates` — opens the create form, counts options in the shipping state dropdown, asserts count == 51 (50 states + blank placeholder)

## 4. Verification

- [x] 4.1 Run `dotnet build` — confirm zero errors
- [x] 4.2 Run `dotnet test FarmAppAspire.Tests.Unit` — all pass (no changes expected, confirm)
- [x] 4.3 Run `dotnet test FarmAppAspire.Tests` — all pass (no changes expected, confirm)
- [x] 4.4 Run `dotnet test FarmAppAspire.Tests.UI` — all pass including new dropdown tests
