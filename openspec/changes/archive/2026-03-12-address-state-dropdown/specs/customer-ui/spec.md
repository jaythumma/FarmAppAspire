## MODIFIED Requirements

### Requirement: State field in address sections is a constrained dropdown
The system SHALL present the State field in both the shipping and billing address sections as a dropdown (`<select>`) populated with the 50 US states, showing each state's full name as the visible label and storing its 2-letter abbreviation as the value. The dropdown SHALL include a blank placeholder option ("— select state —") so that an empty submission is caught by existing required-field validation.

#### Scenario: Shipping state field shows dropdown with US states
- **WHEN** a user opens the create customer form
- **THEN** the State field in the Shipping Address section renders as a dropdown listing all 50 US states

#### Scenario: Billing state field shows dropdown when billing fields are visible
- **WHEN** a user unchecks "Same as shipping address" on the create form
- **THEN** the State field in the Billing Address section also renders as a dropdown listing all 50 US states

#### Scenario: Selecting a state stores the 2-letter abbreviation
- **WHEN** a user selects "Illinois" from the State dropdown
- **THEN** the value stored and submitted is "IL"

#### Scenario: Empty state selection fails required validation
- **WHEN** a user submits the form without selecting a state
- **THEN** the system displays a validation error for the State field and does not submit
