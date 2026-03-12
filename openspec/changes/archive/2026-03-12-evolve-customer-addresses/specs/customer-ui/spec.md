## MODIFIED Requirements

### Requirement: Create and edit customer forms are accessible to Manager and above
The system SHALL provide create and edit forms that respect the role permission matrix. The create form SHALL include a mandatory shipping address section and an optional billing address section with a "same as shipping" toggle.

#### Scenario: Manager creates a wholesale customer
- **WHEN** a `Manager` completes the create customer form with type `Wholesale`
- **THEN** the system shows company-specific fields (Company Name, Tax ID, Payment Terms) in addition to shared fields, and `Company Name` is marked as required

#### Scenario: CompanyName is required when Wholesale is selected
- **WHEN** a user selects type `Wholesale` on the create or edit form
- **THEN** the `Company Name` field is marked required and the form cannot be submitted without it

#### Scenario: Retail customer form hides wholesale-only fields
- **WHEN** a user selects type `Retail` on the create form
- **THEN** the fields `Company Name`, `Tax ID`, and `Payment Terms` are hidden

#### Scenario: Shipping address section is always visible and required
- **WHEN** a user is on the create customer form
- **THEN** a shipping address section is visible with all address fields required; the form cannot be submitted without a valid shipping address

#### Scenario: Billing "same as shipping" toggle is checked by default
- **WHEN** a user opens the create customer form
- **THEN** the billing section shows a checked "Same as shipping address" checkbox and billing address fields are hidden

#### Scenario: Unchecking billing toggle reveals billing address fields
- **WHEN** a user unchecks "Same as shipping address"
- **THEN** the billing address fields appear as optional inputs

#### Scenario: Staff cannot access create form
- **WHEN** a `Staff` user navigates to `/customers/create`
- **THEN** the system renders a "not authorized" message

### Requirement: Customer detail page shows shipping and billing address sections
The system SHALL display a dedicated shipping address section and a billing address section on the customer detail page. The billing section SHALL render one of three states based on `BillingUsesShipping` and the presence of a billing address record.

#### Scenario: Billing same as shipping — shows badge
- **WHEN** a customer's `BillingUsesShipping` is `true`
- **THEN** the billing address section displays "Same as shipping address" and no separate address card

#### Scenario: Billing address record exists — shows card
- **WHEN** `BillingUsesShipping` is `false` and a Billing address record exists
- **THEN** the billing section displays the billing address card

#### Scenario: Billing undefined — shows not-set prompt
- **WHEN** `BillingUsesShipping` is `false` and no Billing address record exists
- **THEN** the billing section displays "Billing address: not set" with action links "Add billing address" and "Use shipping address"
