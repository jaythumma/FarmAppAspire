## ADDED Requirements

### Requirement: Customer billing address state is tracked explicitly
The system SHALL maintain a `BillingUsesShipping` boolean flag on each customer record that records whether the customer's billing address is the same as their shipping address.

#### Scenario: New customer defaults billing to shipping
- **WHEN** a customer is created with `billingUsesShipping: true`
- **THEN** the system stores `BillingUsesShipping = true` and creates no separate billing address record

#### Scenario: New customer with explicit billing address
- **WHEN** a customer is created with `billingUsesShipping: false` and a `billingAddress` is provided
- **THEN** the system stores `BillingUsesShipping = false` and creates a Billing address record linked to the customer

#### Scenario: Billing state exposed in customer detail
- **WHEN** `GET /customers/{id}` is requested
- **THEN** the response includes `billingUsesShipping: bool` alongside the existing address list

### Requirement: Updating a shipping address clears the billing-uses-shipping flag
The system SHALL automatically set `BillingUsesShipping = false` when a Shipping (or Both) type address is updated and the customer currently has `BillingUsesShipping = true`.

#### Scenario: Shipping address updated with flag set
- **WHEN** a Manager submits `PUT /customers/{id}/addresses/{aid}` for an address with `Type = Shipping`
- **AND** the customer's `BillingUsesShipping` is `true`
- **THEN** the system updates the address AND sets `BillingUsesShipping = false` on the customer atomically

#### Scenario: Shipping address updated with flag already false
- **WHEN** a Manager updates a Shipping address and `BillingUsesShipping` is already `false`
- **THEN** the system updates the address only; `BillingUsesShipping` remains `false`

#### Scenario: Billing address updated does not affect flag
- **WHEN** a Manager updates an address with `Type = Billing`
- **THEN** `BillingUsesShipping` is not modified regardless of its current value
