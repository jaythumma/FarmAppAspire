# FarmAppAspire – Copilot Instructions

# Requirements

- Write clear, self-documenting code
- Keep abstractions simple and focused
- Minimize dependencies and coupling
- Use modern C# features appropriately
- Any code you commit MUST compile, and new and existing tests related to the change MUST pass.

# Implement UI web pages to handle features being implemented

- create or update tests
- ensure all tests pass using test driven red/green development
- ensure 90% code coverage
- Follow TDD best practices: write a failing test first, then implement the minimum code to pass the test, then refactor while keeping tests green.


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
- Unit and Integration tests should pass when code changes are made; 
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
