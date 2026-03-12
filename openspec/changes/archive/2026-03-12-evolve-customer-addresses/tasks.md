## 1. Customer Entity & Migration

- [x] 1.1 Add `BillingUsesShipping` bool property to `Customer.cs` (default `false`)
- [x] 1.2 Add `AddressFields` record to `CustomerDtos.cs` with properties: `Line1`, `Line2?`, `City`, `State`, `PostalCode`, `Country` — all with `[Required]` except `Line2`
- [x] 1.3 Update `CreateCustomerRequest` to add `[Required] AddressFields ShippingAddress`, `bool BillingUsesShipping = true`, and `AddressFields? BillingAddress`
- [x] 1.4 Update `CustomerDetailDto` to add `bool BillingUsesShipping`
- [x] 1.5 Update `CustomerMappings.ToDetailDto()` to include `BillingUsesShipping`
- [x] 1.6 Run `dotnet ef migrations add AddBillingUsesShipping --project FarmAppAspire.CustomerService` to generate the migration
- [x] 1.7 Verify the migration sets `defaultValue: false` for the new column

## 2. CustomerService API — Create Handler

- [x] 2.1 In `POST /customers` handler, add guard: `if (req.Type == Wholesale && string.IsNullOrWhiteSpace(req.CompanyName)) return Results.Problem(...)`
- [x] 2.2 After creating the `Customer` record, create a `CustomerAddress` from `req.ShippingAddress` with `Type = Shipping`, `Label = "Shipping"`, `IsDefault = true`, and `CreatedAt = utcNow`
- [x] 2.3 If `req.BillingUsesShipping == false` and `req.BillingAddress != null`, create a second `CustomerAddress` from `req.BillingAddress` with `Type = Billing`, `Label = "Billing"`, `IsDefault = false`
- [x] 2.4 Set `customer.BillingUsesShipping = req.BillingUsesShipping` before saving
- [x] 2.5 Ensure both address records and the customer are saved in a single `SaveChangesAsync` call

## 3. CustomerService API — Update Customer Handler

- [x] 3.1 In `PUT /customers/{id}` handler, add guard: `if (customer.Type == Wholesale && string.IsNullOrWhiteSpace(req.CompanyName)) return Results.Problem(...)`

## 4. CustomerService API — Update Address Handler

- [x] 4.1 In `PUT /customers/{id}/addresses/{aid}` handler, after updating the address fields, check: `if ((address.Type == Shipping || address.Type == Both) && customer.BillingUsesShipping)`
- [x] 4.2 If the condition in 4.1 is true, set `customer.BillingUsesShipping = false`
- [x] 4.3 Load the parent `Customer` in the handler (currently only loads `CustomerAddress`) so the flag can be read and written
- [x] 4.4 Ensure address update and customer flag change are saved atomically in a single `SaveChangesAsync`

## 5. Web — CustomerApiClient

- [x] 5.1 Update `CreateCustomerAsync` method signature and request type to include `ShippingAddress`, `BillingUsesShipping`, and optional `BillingAddress`
- [x] 5.2 Update `CustomerDetail` record (or equivalent DTO) in the client to include `BillingUsesShipping`

## 6. Web — CreateEdit.razor

- [x] 6.1 Add `ShippingAddressModel` sub-model to `CustomerFormModel` with the five required fields
- [x] 6.2 Add `BillingUsesShipping bool` (default `true`) and `BillingAddressModel?` to `CustomerFormModel`
- [x] 6.3 Implement `IValidatableObject` on `CustomerFormModel`: return validation error for `CompanyName` when `Type == "Wholesale"` and `CompanyName` is empty
- [x] 6.4 Add shipping address form section (always visible): label "Shipping Address", fields Line1, Line2 (optional), City, State, PostalCode, Country — all required except Line2
- [x] 6.5 Add billing section below shipping: checkbox "Same as shipping address" bound to `BillingUsesShipping`
- [x] 6.6 When `BillingUsesShipping == false`, render billing address fields (same field set as shipping, all optional at the form level)
- [x] 6.7 Update `SaveAsync` for create path to pass `ShippingAddress`, `BillingUsesShipping`, and `BillingAddress` to `CreateCustomerAsync`
- [x] 6.8 On edit load, populate `BillingUsesShipping` from the loaded customer detail; populate billing address fields if a Billing address record exists in `detail.Addresses`
- [x] 6.9 On edit save, if billing address has changed and `BillingUsesShipping == false`, call `PUT /customers/{id}/addresses/{billingAddressId}` or `POST` a new billing address

## 7. Web — Detail.razor

- [x] 7.1 Replace the current flat addresses section with separate "Shipping Address" and "Billing Address" sections
- [x] 7.2 Shipping address section: show the address card for the address with `Type == Shipping`
- [x] 7.3 Billing address section — state 1 (`BillingUsesShipping == true`): show a `<span class="badge bg-info">Same as shipping address</span>` badge
- [x] 7.4 Billing address section — state 2 (`BillingUsesShipping == false` and a Billing address record exists): show the billing address card
- [x] 7.5 Billing address section — state 3 (`BillingUsesShipping == false` and no Billing address): show "Billing address: not set" with two action links — "Add billing address" (→ navigates to a billing address form) and "Use shipping address" (→ sets `BillingUsesShipping = true` via API call)
- [x] 7.6 Wire the "Use shipping address" action to call `PATCH` or `PUT` on the customer to set `BillingUsesShipping = true` and re-load the detail

## 8. Unit Tests (FarmAppAspire.Tests.Unit)

- [x] 8.1 Add `CustomerValidationTests.cs`: test that `CompanyName` is required when `Type == Wholesale`
- [x] 8.2 Add test: `CompanyName` NOT required when `Type == Retail`
- [x] 8.3 Add test: `CustomerFormModel` `IValidatableObject` returns error for missing `CompanyName` on Wholesale
- [x] 8.4 Add test: `BillingUsesShipping` defaults to `true` in `CreateCustomerRequest`

## 9. Integration Tests (FarmAppAspire.Tests)

- [x] 9.1 Update `ValidCustomer()` helper to include a valid `ShippingAddress` in `CustomerEndpointTests.cs`
- [x] 9.2 Add test: `PostCustomer_Returns400_WhenShippingAddressMissing`
- [x] 9.3 Add test: `PostCustomer_Returns400_WhenWholesaleWithoutCompanyName`
- [x] 9.4 Add test: `PostCustomer_Wholesale_CreatesWithCompanyName`
- [x] 9.5 Add test: `PostCustomer_CreatesShippingAddressAtomically` — verify shipping address record exists after customer creation
- [x] 9.6 Add test: `PostCustomer_BillingUsesShipping_True_NoSeparateBillingRecord`
- [x] 9.7 Add test: `PostCustomer_BillingUsesShipping_False_CreatesBillingRecord`
- [x] 9.8 Add test: `GetCustomer_ExposesBillingUsesShipping`
- [x] 9.9 Add test: `PutAddress_Shipping_ClearsBillingUsesShippingFlag`
- [x] 9.10 Add test: `PutAddress_Billing_DoesNotAffectBillingUsesShippingFlag`
- [x] 9.11 Add test: `PutAddress_Shipping_WhenFlagAlreadyFalse_NoChange`
- [x] 9.12 Add test: `PutCustomer_Returns400_WhenWholesaleWithoutCompanyName`

## 10. UI Tests (FarmAppAspire.Tests.UI)

- [x] 10.1 Update `FarmAdmin_CanCreateCustomer_AppearsInList` to fill in the shipping address section
- [x] 10.2 Add test: `CreateForm_ShowsShippingAddressSection`
- [x] 10.3 Add test: `CreateForm_BillingToggle_CheckedByDefault_HidesBillingFields`
- [x] 10.4 Add test: `CreateForm_UncheckBillingToggle_ShowsBillingFields`
- [x] 10.5 Add test: `CreateForm_WholesaleType_CompanyNameRequired`
- [x] 10.6 Add test: `CreateForm_SubmitWithoutShipping_ShowsValidationError`
- [x] 10.7 Add test: `DetailPage_BillingUsesShipping_ShowsSameAsShippingBadge`
- [x] 10.8 Add test: `DetailPage_BillingUndefined_ShowsNotSetPrompt`

## 11. Verification

- [x] 11.1 Run `dotnet build` — confirm zero errors
- [x] 11.2 Run `dotnet test FarmAppAspire.Tests.Unit` — all pass
- [x] 11.3 Run `dotnet test FarmAppAspire.Tests` — all pass
- [x] 11.4 Run `dotnet test FarmAppAspire.Tests.UI` — all pass
- [x] 11.5 Run the app; create a Wholesale customer and confirm CompanyName is required in UI
- [x] 11.6 Create a customer, uncheck "Same as shipping", add a billing address — confirm both addresses are visible in detail
- [x] 11.7 Edit the shipping address of a customer with `BillingUsesShipping = true` — confirm detail page switches to "not set" billing state
