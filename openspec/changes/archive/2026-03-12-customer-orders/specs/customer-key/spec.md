## ADDED Requirements

### Requirement: Every customer has a unique CustomerKey derived from display name and location
The system SHALL generate a CustomerKey for each customer by combining an abbreviation of the customer's DisplayName with an abbreviation of their primary shipping address City and State. The CustomerKey SHALL be stored on the Customer record and used in all invoice labels.

#### Scenario: CustomerKey is generated from display name and city/state
- **WHEN** a customer has DisplayName = "Mangrove Foods" and primary shipping address in Chicago, IL
- **THEN** the system generates a CustomerKey of the form `MAN-CHI-IL` (exact abbreviation rules defined in the algorithm)

#### Scenario: CustomerKey is stored and reused
- **WHEN** a CustomerKey is generated for a customer
- **THEN** it is persisted on the Customer record and used unchanged in all subsequent invoice labels

### Requirement: CustomerKey abbreviation follows a deterministic algorithm
The system SHALL derive the name component of the CustomerKey by taking the first three letters of each significant word in the DisplayName (ignoring common words such as "LLC", "Inc", "Co", "The"), uppercased and concatenated to a maximum of six characters. The location component SHALL be the first three letters of the City name, uppercased, followed by a hyphen and the two-letter State abbreviation.

#### Scenario: Single-word display name produces a three-letter prefix
- **WHEN** DisplayName = "Sunrise" and City = "Dallas", State = "TX"
- **THEN** CustomerKey = `SUN-DAL-TX`

#### Scenario: Multi-word display name concatenates significant word initials
- **WHEN** DisplayName = "Green Valley Foods LLC" and City = "Austin", State = "TX"
- **THEN** common words ("LLC") are ignored; key derived from "Green Valley Foods" → `GVF-AUS-TX` or first-three-letter form per algorithm

### Requirement: CustomerKey collisions are detected and resolved by staff
The system SHALL detect when a newly generated CustomerKey matches an existing key for a different customer. When a collision is detected, the system SHALL flag it and require staff to manually provide a unique alternative key before the customer can receive invoices.

#### Scenario: Duplicate key is detected at generation time
- **WHEN** the system generates a CustomerKey that already exists for another customer
- **THEN** the system flags the customer record as needing a manual key resolution and does not auto-assign the duplicate

#### Scenario: Staff resolves a key collision
- **WHEN** staff provides an alternate CustomerKey for a customer with a detected collision
- **THEN** the system stores the staff-provided key and clears the collision flag

### Requirement: CustomerKey is updated only by explicit staff action
The system SHALL NOT automatically regenerate the CustomerKey if a customer's DisplayName or address changes after the key has been assigned. Staff MUST explicitly request a key update, which triggers collision detection again.

#### Scenario: Renaming a customer does not automatically change the CustomerKey
- **WHEN** staff updates a customer's DisplayName
- **THEN** the existing CustomerKey is preserved unchanged

#### Scenario: Staff can explicitly request a CustomerKey regeneration
- **WHEN** staff requests a CustomerKey regeneration for a customer
- **THEN** the system derives a new key, checks for collisions, and either assigns it or flags it for resolution
