## 1. CustomerApiClient — Contact Methods

- [x] 1.1 Verify `ContactDto` and `CreateContactRequest`/`UpdateContactRequest` records exist in `CustomerApiClient.cs`; add any that are missing
- [x] 1.2 Add `CreateContactAsync(Guid customerId, CreateContactRequest request, CancellationToken)` → `ContactDto?` targeting `POST /customers/{id}/contacts`
- [x] 1.3 Add `UpdateContactAsync(Guid customerId, Guid contactId, UpdateContactRequest request, CancellationToken)` → `ContactDto?` targeting `PUT /customers/{id}/contacts/{cid}`
- [x] 1.4 Add `DeleteContactAsync(Guid customerId, Guid contactId, CancellationToken)` → `bool` targeting `DELETE /customers/{id}/contacts/{cid}`, returning `true` on HTTP 204

## 2. Unit Tests — CustomerApiClient Contact Methods

- [x] 2.1 Add `CustomerApiClientContactTests.cs` in `FarmAppAspire.Tests.Unit/Web/`
- [x] 2.2 Test `CreateContactAsync` returns a deserialized `ContactDto` when the mock HTTP handler returns HTTP 201 with JSON body
- [x] 2.3 Test `UpdateContactAsync` returns a deserialized `ContactDto` when the mock HTTP handler returns HTTP 200 with JSON body
- [x] 2.4 Test `DeleteContactAsync` returns `true` when the mock HTTP handler returns HTTP 204
- [x] 2.5 Test `DeleteContactAsync` returns `false` when the mock HTTP handler returns HTTP 404

## 3. Detail.razor — Contact Interaction State

- [x] 3.1 Add private mutable `ContactForm` inner class (or record with mutable properties) to `Detail.razor` with fields: `FirstName`, `LastName`, `Role`, `Email`, `Phone`, `Mobile`, `IsPrimary`
- [x] 3.2 Add state fields: `_contactForm` (ContactForm), `_editingContactId` (Guid?), `_showContactForm` (bool), `_pendingDeleteId` (Guid?), `_contactError` (string?)

## 4. Detail.razor — Contacts Section UI

- [x] 4.1 Replace the existing static contacts table with an interactive version: add "Edit" and "Delete" buttons per row (visible to FarmAdmin only, via `<AuthorizeView>`)
- [x] 4.2 Add the two-step delete confirmation inline per row: first click sets `_pendingDeleteId`; row shows "Confirm?" + "Cancel"; second click calls delete API
- [x] 4.3 Add "Add Contact" button above the contacts table (FarmAdmin only) that sets `_showContactForm = true` and resets `_contactForm` with Role defaulting to `ContactRole.Primary`
- [x] 4.4 Add inline `<EditForm>` that renders when `_showContactForm` is true, containing inputs for all contact fields, a Save button, and a Cancel button
- [x] 4.5 Display a warning hint when `_customer.Type == CustomerType.Wholesale` and `_customer.Contacts` is empty (requirement: wholesale contact hint)
- [x] 4.6 Display `_contactError` as an inline `alert-danger` when non-null; add a dismiss button that clears it

## 5. Detail.razor — Contact Action Methods

- [x] 5.1 Add `StartAddContact()` method: resets form, sets `_editingContactId = null`, `_showContactForm = true`
- [x] 5.2 Add `StartEditContact(ContactDto contact)` method: populates `_contactForm` from contact, sets `_editingContactId = contact.Id`, `_showContactForm = true`
- [x] 5.3 Add `SaveContactAsync()` async method: if `_editingContactId` is set calls `UpdateContactAsync`; otherwise calls `CreateContactAsync`; on success patches `_customer.Contacts` and closes form; on failure sets `_contactError`
- [x] 5.4 Add `ConfirmDeleteContactAsync(Guid contactId)` async method: calls `DeleteContactAsync`; on success removes the contact from `_customer.Contacts`; on failure sets `_contactError`; clears `_pendingDeleteId`

## 6. Verification

- [x] 6.1 Run `dotnet build` — confirm zero errors
- [x] 6.2 Run `dotnet test FarmAppAspire.Tests.Unit` — confirm all new and existing tests pass
- [ ] 6.3 Start the app via AppHost; navigate to a customer detail page and verify Add / Edit / Delete contact interactions work end-to-end
