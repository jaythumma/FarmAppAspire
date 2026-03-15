## Why

The Customer Detail page displays contacts in a read-only table with no way to add, edit, or delete them from the UI. The backend API already exposes full CRUD endpoints for contacts (`POST/PUT/DELETE /customers/{id}/contacts`), but the frontend has no interactive UI wired to them, making contact management impossible without direct API calls.

## What Changes

- Add an **Add Contact** button to the Contacts section on the Customer Detail page, opening an inline form with fields for name, email, phone/mobile, role, and primary flag.
- Make each existing contact row **editable** — an Edit button opens the same inline form pre-populated with that contact's data.
- Add a **Delete** button per contact row with a confirmation prompt.
- Add `CreateContactAsync`, `UpdateContactAsync`, and `DeleteContactAsync` methods to `CustomerApiClient` so the Blazor component can call the existing backend endpoints.
- Contact selection is **optional for Retail** customers and **required for Wholesale** customers; the UI enforces this through a validation hint.
- When no role is specified the default is **Primary**.
- Add a `ContactDto` record to `CustomerApiClient.cs` (mirroring the one already defined in the service) if not already present.

## Capabilities

### New Capabilities

- `customer-contact-management`: Inline add, edit, and delete of contacts on the Customer Detail page, with role selection, primary-flag toggle, and per-customer-type validation hints.

### Modified Capabilities

*(none — no existing spec-level behaviour changes)*

## Impact

- **`FarmAppAspire.Web/Components/Pages/Customers/Detail.razor`** — Contacts section gains Add/Edit/Delete controls, an inline form, and error/confirmation state.
- **`FarmAppAspire.Web/CustomerApiClient.cs`** — Three new async methods for contact CRUD; `ContactDto` record verified/added.
- **`FarmAppAspire.Tests.Unit/Web/`** — New unit tests for the three new `CustomerApiClient` methods and for the contact form validation logic.
- No backend API changes; no database migrations; no new NuGet packages.
