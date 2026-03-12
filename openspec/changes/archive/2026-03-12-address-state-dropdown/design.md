## Context

The address form in `CreateEdit.razor` has two address sections (shipping, and optionally billing). Both currently use `<InputText @bind-Value="...State" class="form-control" />` for the State field. The `AddressSubModel` class validates `State` with `[Required, MinLength(1)]` but does nothing to constrain the value to valid US states.

The `UsStateSelect` component needs to integrate with Blazor's `EditForm` / `EditContext` so that the bound value participates in the existing `IValidatableObject` validation flow.

## Goals / Non-Goals

**Goals:**
- Replace both State text inputs (shipping and billing) with a single reusable `UsStateSelect` Blazor component.
- Keep the component simple: a plain `<select>` / `<InputSelect>` bound via `@bind-Value`.
- Hardcode the 50 state pairs (abbreviation → full name) directly in the component — no external data source.
- Ensure the dropdown renders with a blank "— select state —" placeholder option so validation still catches an empty submission.

**Non-Goals:**
- Supporting non-US addresses or territories (DC, PR, etc.) — keep to 50 states only.
- Storing the full state name — only the 2-letter abbreviation is stored, matching the existing string column.
- Any API, migration, or data-backfill changes.

## Decisions

### D1 — Blazor `InputSelect<string>` not a raw `<select>`
**Decision:** Implement `UsStateSelect` as a thin wrapper around `<InputSelect TValue="string">` rather than a raw HTML `<select>`.

**Rationale:** `InputSelect` registers itself with the parent `EditContext`, meaning field-level validation errors and CSS validation classes (`valid`/`invalid`) work automatically. A raw `<select>` would bypass Blazor's validation pipeline.

### D2 — Two-way bind via `@bind-Value` parameter
**Decision:** Expose a `Value` / `ValueChanged` / `ValueExpression` parameter triple (standard Blazor bindable component pattern) so callers write `<UsStateSelect @bind-Value="_model.ShippingAddress.State" class="form-control" />`.

**Rationale:** Matches the pattern already used by all other `<InputText>` and `<InputSelect>` fields in the form. No special wiring needed in `CreateEdit.razor`.

**Alternative considered:** Inheriting from `InputBase<string>`. Rejected — adds unnecessary complexity for a purely presentational wrapper; the composition approach (`InputSelect` inside the component) is simpler.

### D3 — Abbreviation as stored value, full name in display label
**Decision:** Each `<option value="IL">Illinois</option>` — the 2-letter abbreviation is the `value` attribute, the full name is the visible label.

**Rationale:** Existing `CustomerAddress.State` stores short codes (e.g. "IL"). Displaying full names in the dropdown improves legibility without changing the stored format.

### D4 — Blank placeholder as first option
**Decision:** First option is `<option value="">— select state —</option>`.

**Rationale:** Ensures a new form starts with no state selected, so the existing `[Required]` / `IValidatableObject` logic correctly flags an empty state submission.

## Risks / Trade-offs

- **Existing stored data with non-abbreviation values** (e.g. "Illinois", "il") will not match any dropdown option and will render as the blank placeholder when an existing customer is edited. This is acceptable — the admin will need to re-select. Mitigation: none required; the current codebase has no production data.
- **Territories (DC, PR, GU…)** are excluded by design. If needed later, adding them is a one-line change in the component.

## Migration Plan

1. Create `UsStateSelect.razor` in `FarmAppAspire.Web/Components/Shared/`.
2. Replace the two `<InputText>` State inputs in `CreateEdit.razor` with `<UsStateSelect>`.
3. Update `FillShippingAddressAsync` in UI tests to use `SelectOptionAsync` instead of `FillAsync` for the state field.
4. Add one UI test scenario.
5. Build and run all three test suites.
