## ADDED Requirements

### Requirement: Customer list endpoint returns paginated results
The `GET /customers` endpoint SHALL return a paginated response with `total`, `page`, `size`, and `items` fields.

#### Scenario: Empty customer list
- **WHEN** `GET /customers` is called and no customers exist
- **THEN** response is HTTP 200 with `total: 0` and an empty `items` array

#### Scenario: Pagination returns correct slice
- **WHEN** 5 customers exist and `GET /customers?page=2&size=2` is called
- **THEN** response contains exactly 2 items and `total: 5`

### Requirement: Customer detail endpoint returns customer with nested resources
The `GET /customers/{id}` endpoint SHALL return the full customer record including all contacts and addresses.

#### Scenario: Customer not found returns 404
- **WHEN** `GET /customers/{id}` is called with an ID that does not exist
- **THEN** response is HTTP 404

#### Scenario: Customer detail includes contacts and addresses
- **WHEN** a customer with one contact and one address exists and `GET /customers/{id}` is called
- **THEN** response is HTTP 200 and body contains the contact and address nested within the customer

### Requirement: Customer creation propagates user identity to CreatedBy
The `POST /customers` endpoint SHALL set the `CreatedBy` field to the value of the `X-User-Id` request header and return HTTP 201 with the created resource.

#### Scenario: Customer created with valid payload and X-User-Id header
- **WHEN** `POST /customers` is called with a valid `CreateCustomerRequest` body and `X-User-Id: test-user-1` header
- **THEN** response is HTTP 201 and the returned `CustomerDetailDto` has `CreatedBy: "test-user-1"`

#### Scenario: Missing X-User-Id header returns 400
- **WHEN** `POST /customers` is called without the `X-User-Id` header
- **THEN** response is HTTP 400

#### Scenario: Missing required DisplayName returns 400
- **WHEN** `POST /customers` is called with an empty `DisplayName` field
- **THEN** response is HTTP 400

### Requirement: Customer update sets ModifiedBy from X-User-Id header
The `PUT /customers/{id}` endpoint SHALL update the customer record and set `ModifiedBy` to the `X-User-Id` header value.

#### Scenario: Customer updated with valid payload
- **WHEN** `PUT /customers/{id}` is called with a valid `UpdateCustomerRequest` and `X-User-Id: updater-1` header
- **THEN** response is HTTP 200 and `ModifiedBy` equals `"updater-1"`

#### Scenario: Update on unknown customer returns 404
- **WHEN** `PUT /customers/{id}` is called with an ID that does not exist
- **THEN** response is HTTP 404

### Requirement: Customer deletion removes customer and all nested resources
The `DELETE /customers/{id}` endpoint SHALL remove the customer and cascade-delete all associated contacts and addresses.

#### Scenario: Customer deleted returns 204
- **WHEN** `DELETE /customers/{id}` is called for an existing customer with contacts and addresses
- **THEN** response is HTTP 204 and the customer, its contacts, and its addresses no longer exist in the database

#### Scenario: Delete on unknown customer returns 404
- **WHEN** `DELETE /customers/{id}` is called with an ID that does not exist
- **THEN** response is HTTP 404

### Requirement: First address added to a customer is automatically set as default
The `POST /customers/{id}/addresses` endpoint SHALL set `IsDefault = true` on the first address added to a customer regardless of the `IsDefault` field in the request.

#### Scenario: First address becomes default automatically
- **WHEN** a customer has no addresses and `POST /customers/{id}/addresses` is called with `IsDefault: false`
- **THEN** the created address has `IsDefault: true`

### Requirement: Setting a new default address clears the previous default
The `POST /customers/{id}/addresses` endpoint SHALL clear `IsDefault` on all existing addresses when a new address is posted with `IsDefault: true`.

#### Scenario: New default address demotes previous default
- **WHEN** a customer has one address with `IsDefault: true` and a second address is posted with `IsDefault: true`
- **THEN** the first address has `IsDefault: false` and the newly created address has `IsDefault: true`

### Requirement: Deleting the default address promotes the oldest remaining address
The `DELETE /customers/{id}/addresses/{aid}` endpoint SHALL promote the oldest remaining address (by `CreatedAt`) to default when the deleted address was the default.

#### Scenario: Default address deleted promotes oldest remaining
- **WHEN** a customer has two addresses (A created first, B created second and set as default) and B is deleted
- **THEN** address A is promoted to `IsDefault: true`

#### Scenario: Non-default address deleted leaves default unchanged
- **WHEN** a customer has a default address A and a non-default address B, and B is deleted
- **THEN** address A remains `IsDefault: true`

### Requirement: Contact CRUD operations are scoped to their parent customer
Contact endpoints SHALL return HTTP 404 when the parent customer ID does not match the contact's `CustomerId`.

#### Scenario: Add contact to existing customer
- **WHEN** `POST /customers/{id}/contacts` is called with a valid `CreateContactRequest`
- **THEN** response is HTTP 201 and the contact is retrievable under the customer

#### Scenario: Delete contact with wrong customer ID returns 404
- **WHEN** `DELETE /customers/{id}/contacts/{cid}` is called where `{id}` is not the contact's parent
- **THEN** response is HTTP 404
