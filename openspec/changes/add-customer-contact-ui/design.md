## Context

The Customer Detail page (`/customers/{id}`) already renders a read-only contacts table from the `CustomerDetailDto.Contacts` collection. The CustomerService exposes complete CRUD endpoints (`GET/POST/PUT/DELETE /customers/{id}/contacts`), and the DTOs (`ContactDto`, `CreateContactRequest`, `UpdateContactRequest`) are already defined. The `CustomerApiClient` in the Web project currently has **no methods** targeting those endpoints, so the frontend has no way to manage contacts today.

`CustomerContact.Role` is typed as `ContactRole` (Primary, Billing, Purchasing, Shipping, Secondary, Other). Contact selection is optional for Retail customers and carries an implicit business expectation for Wholesale accounts.

## Goals / Non-Goals

**Goals:**
- Add `CreateContactAsync`, `UpdateContactAsync`, and `DeleteContactAsync` methods to `CustomerApiClient`.
- Upgrade the Contacts section in `Detail.razor` from a read-only table to an interactive panel with Add / Edit / Delete controls.
- Inline form (not a separate page) so the user stays in context.
- Show a validation hint when a Wholesale customer has zero contacts.
- Default role to `Primary` when no role is selected.
- Guard Delete with a confirmation step to prevent accidental removal.
- Unit-test all three new `CustomerApiClient` methods.

**Non-Goals:**
- A separate `/contacts` management page — the change is scoped to the Detail view.
- Reordering / drag-and-drop contact priority.
- Merging or deduplication of contacts.
- Backend API changes — all required endpoints already exist.
- Database migrations.

## Decisions

### D1 — Inline form vs. modal dialog

**Choice:** Inline collapsible form rendered directly inside the Contacts card.

**Rationale:** `Detail.razor` uses `@rendermode InteractiveServer`, so DOM manipulation via JS modals (Bootstrap `modal.show()`) is possible but adds coupling to JS interop. An inline Blazor form keeps the component fully server-side and consistent with how addresses are handled elsewhere in the project. The pattern is simpler to test.

**Alternative considered:** Bootstrap modal triggered by JS interop. Rejected because it requires `IJSRuntime` injection and async JS calls just to show/hide a form, adding complexity with no user-experience gain in a Blazor Server app.

### D2 — Reuse `ContactDto` record already in `CustomerApiClient.cs`

**Choice:** The `ContactDto` record already exists in `CustomerApiClient.cs`; use it for the form model and API responses.

**Rationale:** Avoids duplication. The Web project already defines `ContactRole` as a client-side enum (identical to the service enum). No new types are needed.

**Alternative considered:** Add a dedicated `ContactFormModel` class for two-way binding. Unnecessary given `ContactDto` is immutable (record); a mutable local class is only needed for the `EditForm` binding, so a private inner class `ContactForm` in the code-behind is sufficient.

### D3 — Delete confirmation via inline state flag (no JS confirm)

**Choice:** Per-row `_pendingDeleteId` (Guid?) field; clicking Delete once sets it, rendering "Confirm?" and a Cancel button; second click executes the DELETE call.

**Rationale:** Pure Blazor, no JS interop, easily unit-tested. Consistent with the key-edit confirm pattern already in `Detail.razor`.

**Alternative considered:** `window.confirm()` via `JSRuntime.InvokeAsync<bool>`. Rejected for the same interop-complexity reason as D1.

### D4 — `CustomerApiClient` contact methods follow existing pattern

**Choice:** `CreateContactAsync` returns `ContactDto?`; `UpdateContactAsync` returns `ContactDto?`; `DeleteContactAsync` returns `bool`.

**Rationale:** Mirrors `CreateAddressAsync`/`UpdateAddressAsync`/`DeleteCustomerAsync` already in the client. Consistent return types mean callers can null-check without special casing.

## Risks / Trade-offs

- **Risk:** Wholesale contact validation is a UI hint only, not enforced by the backend. → **Mitigation:** Backend can add a validation rule in a future change; the spec makes the hint requirement explicit so it is not forgotten.
- **Risk:** Simultaneous edits from two browser tabs could cause a stale `_customer.Contacts` list after a mutation. → **Mitigation:** After each successful mutation the component re-fetches `_customer` from the API (or patches the in-memory list from the returned `ContactDto`), keeping the view consistent.
- **Trade-off:** Inline form doubles the height of the Contacts card during editing. Acceptable for a back-office admin page with typically fewer than 5 contacts.

## Migration Plan

1. Add three contact methods to `CustomerApiClient.cs`.
2. Upgrade the Contacts section in `Detail.razor`:
   a. Replace the static table with an interactive table + Add form.
   b. Add `_contactForm`, `_editingContactId`, `_pendingDeleteId`, `_contactError` fields.
   c. Wire `SaveContactAsync` and `DeleteContactAsync` methods.
3. Add unit tests in `FarmAppAspire.Tests.Unit/Web/` for the three new client methods.
4. Build and run all unit tests — no migrations or deployment steps required.

## Open Questions

*(none)*
