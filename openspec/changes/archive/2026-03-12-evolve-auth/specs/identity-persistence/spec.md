## ADDED Requirements

### Requirement: Store users in Postgres via Identity
The system SHALL persist user accounts using ASP.NET Core Identity backed by a dedicated Postgres database (`identity-db`), including password hashes, lockout state, and role assignments.

#### Scenario: Application starts with empty database
- **WHEN** the application starts and no users exist in the Identity database
- **THEN** the system seeds the four farm roles and a default FarmAdmin account using credentials from environment configuration

#### Scenario: Identity schema migration runs on startup
- **WHEN** the application starts
- **THEN** the system applies any pending EF Core Identity migrations before accepting requests

### Requirement: Default admin account is seeded from environment config
The system SHALL read a default FarmAdmin account's email and password from environment variables (`FARM_ADMIN_EMAIL`, `FARM_ADMIN_PASSWORD`) and create it only if no users exist.

#### Scenario: First-run seed succeeds
- **WHEN** `FARM_ADMIN_EMAIL` and `FARM_ADMIN_PASSWORD` are set and no users exist
- **THEN** the system creates one user with the `FarmAdmin` role and logs a confirmation message

#### Scenario: Seed is skipped when users already exist
- **WHEN** at least one user already exists in the database
- **THEN** the system does NOT modify any existing accounts and proceeds normally
