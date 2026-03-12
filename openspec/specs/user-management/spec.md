## ADDED Requirements

### Requirement: FarmAdmin can create a new user account
The system SHALL allow a `FarmAdmin` user to create a new user account by providing an email address, display name, and assigned role.

#### Scenario: Valid new user created
- **WHEN** a FarmAdmin submits a valid email, display name, and role
- **THEN** the system creates the account, assigns the role, and displays the new user in the user list

#### Scenario: Duplicate email rejected
- **WHEN** a FarmAdmin submits an email address that already exists
- **THEN** the system displays a validation error and does NOT create a duplicate account

#### Scenario: Non-FarmAdmin cannot access user management
- **WHEN** a user without the `FarmAdmin` role navigates to `/admin/users`
- **THEN** the system renders a "not authorized" message and does not display user data

### Requirement: FarmAdmin can assign or change a user's role
The system SHALL allow a `FarmAdmin` to update the role of any user except their own account.

#### Scenario: Role updated successfully
- **WHEN** a FarmAdmin selects a new role for another user and saves
- **THEN** the system updates the role and the change takes effect on the user's next login

#### Scenario: FarmAdmin cannot change their own role
- **WHEN** a FarmAdmin attempts to change their own role
- **THEN** the system displays an error and does NOT modify the role

### Requirement: FarmAdmin can deactivate a user account
The system SHALL allow a `FarmAdmin` to deactivate a user account, preventing future logins without deleting audit history.

#### Scenario: Deactivated user cannot log in
- **WHEN** a deactivated user submits valid credentials
- **THEN** the system rejects the login and displays an "account is inactive" message

#### Scenario: FarmAdmin cannot deactivate their own account
- **WHEN** a FarmAdmin attempts to deactivate their own account
- **THEN** the system displays an error and does NOT deactivate the account

### Requirement: FarmAdmin can reset another user's password
The system SHALL allow a `FarmAdmin` to generate a temporary password for a user account.

#### Scenario: Temporary password generated
- **WHEN** a FarmAdmin triggers a password reset for a user
- **THEN** the system sets a new system-generated temporary password and displays it once to the FarmAdmin to communicate to the user
