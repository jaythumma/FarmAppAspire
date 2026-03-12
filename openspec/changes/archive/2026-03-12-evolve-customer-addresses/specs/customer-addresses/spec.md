## MODIFIED Requirements

### Requirement: Update an address
The system SHALL allow a Manager or FarmAdmin to update any field of an address. Updating a Shipping (or Both) type address when the customer has `BillingUsesShipping = true` SHALL automatically set `BillingUsesShipping = false`.

#### Scenario: Update shipping address clears billing flag
- **WHEN** a Manager submits `PUT /customers/{id}/addresses/{addressId}` for a Shipping-type address
- **AND** the customer's `BillingUsesShipping` is `true`
- **THEN** the system updates the address fields AND sets `Customer.BillingUsesShipping = false` atomically

#### Scenario: Update billing address does not affect flag
- **WHEN** a Manager updates a Billing-type address
- **THEN** the address is updated and `BillingUsesShipping` is unchanged
