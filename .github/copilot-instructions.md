# FarmAppAspire – Copilot Instructions

# Requirements

- Write clear, self-documenting code
- Keep abstractions simple and focused
- Minimize dependencies and coupling
- Use modern C# features appropriately
- Any code you commit MUST compile, and new and existing tests related to the change MUST pass.
- MUST make your best effort to ensure any code changes satisfy those criteria before committing. If for any reason you were unable to build or test code changes, you MUST report that.
- must NOT claim success unless all builds and tests pass as described above.
- Before completing, use the code-review skill to review your code changes. Any issues flagged as errors or warnings should be addressed before completing.
- Run tests locally before committing. Do not rely solely on CI to catch build or test failures.
- If you are unsure about any of the requirements or how to implement a change, ask for clarification before proceeding. Do not make assumptions that could lead to code that does not meet the standards outlined above.
- Ensure code coverage stays above 80% for the entire solution, and above 90% for critical paths, when running Unit and Integration tests. 
- Follow TTD practices: write failing tests first (unit and integration), then implement code to pass those tests. Red Green refactor cycles should be followed to ensure code quality and maintainability.

## Architecture Overview

This is a **.NET 10 Aspire** solution with five projects:

| Project | Role |
|---|---|
| `FarmAppAspire.AppHost` | Aspire orchestration host – the single entry point to run everything |
| `FarmAppAspire.ApiService` | Minimal API backend (no controllers) |
| `FarmAppAspire.Web` | Blazor Server frontend (Interactive Server + SSR) |
| `FarmAppAspire.ServiceDefaults` | Shared cross-cutting config (OTel, service discovery, health checks, resilience) |
| `FarmAppAspire.Tests` | Integration tests using `Aspire.Hosting.Testing` + xUnit v3 |

Infrastructure resources (Redis) are declared in `AppHost/AppHost.cs` using the Aspire fluent API and are provisioned automatically at run time.

## Running the App

**Always run via the AppHost**, not individual projects:
```
dotnet run --project FarmAppAspire.AppHost
```
The AppHost starts Redis, `apiservice`, and `webfrontend` in dependency order (`WaitFor`). The Aspire dashboard is available at the URL printed on startup.

## Critical Patterns

### Service Discovery
The web frontend references the API using the Aspire service name, not a hardcoded URL:
```csharp
client.BaseAddress = new("https+http://apiservice"); // in Web/Program.cs
```
Use `https+http://` scheme to prefer HTTPS with HTTP fallback. Service names must match those registered in `AppHost.cs`.

### ServiceDefaults – call it everywhere
Every service project calls `builder.AddServiceDefaults()` immediately after `WebApplication.CreateBuilder`. This wires OpenTelemetry, resilience (`AddStandardResilienceHandler`), and service discovery together. Do not skip this in new projects.

### Adding a new API endpoint
Add minimal API endpoints directly in `FarmAppAspire.ApiService/Program.cs` using `app.MapGet/MapPost`. No controller classes are used.

### Typed HTTP clients
API communication from the frontend uses typed `HttpClient` wrappers (see `WeatherApiClient.cs`). Use `GetFromJsonAsAsyncEnumerable` for streaming collections. Register with `builder.Services.AddHttpClient<T>`.

### Blazor render modes
- Default pages: static SSR (no `@rendermode` attribute)
- Interactive UI: `@rendermode InteractiveServer` (see `Counter.razor`)
- Async data load with progressive rendering: `@attribute [StreamRendering(true)]` (see `Weather.razor`)
- Output caching via Redis: `@attribute [OutputCache(Duration = 5)]` (requires `builder.AddRedisOutputCache("cache")` in `Web/Program.cs`)

## Code Style & Conventions
- Use **C# records** for immutable data models (e.g., `record Order(Guid Id, string CustomerId, ...)`).
- Prefer **primary constructors** (C# 12+) for dependency injection, Minimize constructor injection
- Use **async/await** throughout; all I/O operations must be async and accept a `CancellationToken`.
- Use **minimal API endpoints** organized as extension methods (e.g., `MapOrderEndpoints()`) on `WebApplication`.
- Group related routes with `MapGroup(prefix)` and annotate with `.WithName()` and `.Produces<T>()` for OpenAPI.
- Follow the `KingsAspire.[ServiceName]` namespace convention.
- Place models in `Models/`, endpoint extensions in `Endpoints/`, and service logic in `Services/`.
- Separate state from behavior, Prefer pure methods
- Prefer composition with interfaces, Use extension methods appropriately
- Design for testability

- ## Data Access Patterns
- Scaffold repositories and DbContexts as needed for each service, but keep them focused on the specific data needs of that service.
- Keep entities narrow and focused; avoid "God objects" that try to represent too much.
- Use EF Core's fluent API to configure relationships and constraints; avoid data annotations for complex configurations.

## Dependency Injection

- Register services in `Program.cs` using the `builder.Services` extensions.
- Prefer `AddScoped<>()` for request-scoped services; use `AddSingleton<>()` only for stateless/thread-safe services.
- Always use constructor injection (or primary constructor parameters) — never use `ServiceLocator` or `IServiceProvider` directly.

## Testing

- Tests use `DistributedApplicationTestingBuilder` to spin up the **full Aspire app** in-process:
```csharp
var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.FarmAppAspire_AppHost>(cancellationToken);
await using var app = await appHost.BuildAsync(cancellationToken);
await app.StartAsync(cancellationToken);
var httpClient = app.CreateHttpClient("webfrontend");
await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken);
```
- Always `WaitForResourceHealthyAsync` before making HTTP assertions. Use `TestContext.Current.CancellationToken` for xUnit v3 cancellation.
- Create tests for both service logic (unit tests) and API endpoints (integration tests).
- Use `Aspire.Hosting.Testing` for integration tests that involve multiple services and real HTTP communication.
- Use `Moq` for mocking dependencies in unit tests.
- Write tests that validate expected behavior and edge cases, not just to increase coverage numbers.
- Use `Playwright` for testing blazor components.
- Unit and Integration tests should pass when code changes are made; 
- Ensure code coverage is at least 90% for critical paths and 80% overall.
- NO NEED to run UI tests always. Can be skipped to faster development when no UI changes are involved.
- UI tests should be run when major UI changes are implemented or when bugs are fixed in the UI layer to ensure that the changes work as expected and do not introduce new issues.
- Do not merge code changes that reduce coverage below these thresholds without a compelling reason and a plan to add more tests.
- Document any significant gaps in test coverage and the rationale for not covering them.
- Do not include trivial tests that only exist to increase coverage numbers; all tests should provide meaningful validation of behavior.
- Run tests:
```
dotnet test FarmAppAspire.Tests
```

## Solution Format

The solution uses `.slnx` (XML-based, `FarmAppAspire.slnx`) instead of the legacy `.sln` format. Use `dotnet sln` commands with the `.slnx` file.

## Key Files
- `FarmAppAspire.AppHost/AppHost.cs` – resource graph, dependencies, health check paths
- `FarmAppAspire.ServiceDefaults/Extensions.cs` – shared OTel/resilience/health config
- `FarmAppAspire.Web/WeatherApiClient.cs` – reference pattern for typed API clients
- `FarmAppAspire.Web/Components/Pages/Weather.razor` – reference pattern for streaming + cached data pages
