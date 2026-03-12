## ADDED Requirements

### Requirement: All test projects collect Cobertura-format coverage on every run
Every test project (`FarmAppAspire.Tests`, `FarmAppAspire.Tests.Unit`) SHALL collect code coverage using Coverlet's `XPlat Code Coverage` data collector, outputting `coverage.cobertura.xml` files.

#### Scenario: Coverage file produced after test run
- **WHEN** `dotnet test --collect:"XPlat Code Coverage"` is run on any test project
- **THEN** a `coverage.cobertura.xml` file is produced in the test results directory

### Requirement: CI runs unit tests, integration tests, and UI tests as separate jobs
The GitHub Actions CI workflow SHALL define separate jobs for unit tests, integration tests, and UI tests so they can be individually identified and optionally parallelised.

#### Scenario: All three test jobs run on push to master
- **WHEN** a commit is pushed to the `master` branch
- **THEN** the `unit-tests`, `integration-tests`, and `ui-tests` jobs all execute

#### Scenario: All three test jobs run on a pull request
- **WHEN** a pull request is opened or updated
- **THEN** the `unit-tests`, `integration-tests`, and `ui-tests` jobs all execute

### Requirement: Playwright browser binaries are cached in CI
The GitHub Actions UI test job SHALL cache Playwright browser binaries using `actions/cache` to avoid re-downloading on every run.

#### Scenario: Cache hit skips browser download
- **WHEN** the Playwright version has not changed since the last run
- **THEN** browser binaries are restored from cache and the install step is skipped

### Requirement: Coverage XML files from all test projects are merged by ReportGenerator
The CI coverage report job SHALL use `dotnet-reportgenerator-globaltool` to merge all Cobertura XML files from the unit and integration test jobs into a single combined HTML report and a single merged Cobertura XML.

#### Scenario: HTML report artifact uploaded after successful tests
- **WHEN** unit and integration test jobs succeed
- **THEN** a `coverage-report` artifact containing the HTML report is uploaded to the GitHub Actions run

### Requirement: Merged coverage is uploaded to Codecov on every CI run
The CI workflow SHALL upload the merged Cobertura XML to Codecov using `codecov/codecov-action`, enabling PR diff-coverage annotations and trend tracking.

#### Scenario: Codecov receives coverage on push and pull request
- **WHEN** the coverage report job completes successfully
- **THEN** coverage data is uploaded to Codecov and a PR comment with coverage delta is posted (on pull requests)

### Requirement: CI fails if combined coverage drops below defined thresholds
A `codecov.yml` configuration file SHALL define coverage thresholds: 90% for `FarmAppAspire.CustomerService` and auth-related files in `FarmAppAspire.Web`, and 80% overall. The CI build SHALL fail if coverage drops below these thresholds.

#### Scenario: Build fails when overall coverage drops below 80%
- **WHEN** a pull request reduces overall combined coverage below 80%
- **THEN** the Codecov status check on the PR fails

#### Scenario: Build fails when CustomerService coverage drops below 90%
- **WHEN** a pull request reduces `FarmAppAspire.CustomerService` coverage below 90%
- **THEN** the Codecov status check on the PR fails

### Requirement: Developer can generate a local HTML coverage report with one command
A documented `dotnet` command sequence SHALL allow any developer to produce the full HTML coverage report locally without requiring Codecov or CI access.

#### Scenario: Local report generated from dotnet test output
- **WHEN** a developer runs `dotnet test --collect:"XPlat Code Coverage"` followed by the `reportgenerator` command
- **THEN** an `index.html` file is produced in the output directory that can be opened in a browser
