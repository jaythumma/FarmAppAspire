# FarmAppAspire 

## Architecture Overview

This is a **.NET 10 Aspire** orchestrated solution

| Project | Role |
|---|---|
| `FarmAppAspire.AppHost` | Aspire orchestration host – the single entry point to run everything |
| `FarmAppAspire.ApiService` | Minimal API backend (no controllers) |
| `FarmAppAspire.Web` | Blazor Server frontend (Interactive Server + SSR) |
| `FarmAppAspire.ServiceDefaults` | Shared cross-cutting config (OTel, service discovery, health checks, resilience) |
| `FarmAppAspire.Tests` | Integration tests using `Aspire.Hosting.Testing` + xUnit v3 |

- Infrastructure resources (Redis) are declared in `AppHost/AppHost.cs` using the Aspire fluent API and are provisioned automatically at run time.
- Services are registered with health check paths, which the AppHost uses to determine when each service is healthy and ready to receive traffic. The AppHost starts services in dependency order based on these health checks.
- The Aspire dashboard provides real-time visibility into resource health, logs, and telemetry. Use it to monitor the app while it's running and to debug test failures.

## Running the App
 - **Always run via the AppHost**, not individual projects:
```
dotnet run --project FarmAppAspire.AppHost
```
- The AppHost starts Redis, `apiservice`, and `webfrontend` in dependency order (`WaitFor`). The Aspire dashboard is available at the URL printed on startup.

## Critical Patterns

### Service Discovery
- The web frontend references the API using the Aspire service name, not a hardcoded URL:
```csharp
client.BaseAddress = new("https+http://apiservice"); // in Web/Program.cs
```
- Use `https+http://` scheme to prefer HTTPS with HTTP fallback. Service names must match those registered in `AppHost.cs`.

### ServiceDefaults – call it everywhere
- Every service project calls `builder.AddServiceDefaults()` immediately after `WebApplication.CreateBuilder`. This wires OpenTelemetry, resilience (`AddStandardResilienceHandler`), and service discovery together. Do not skip this in new projects.
- `AddServiceDefaults` is defined in the `FarmAppAspire.ServiceDefaults` project and should be reused across all services to ensure consistent configuration.


### Adding a new API endpoint
- Add minimal API endpoints directly in `FarmAppAspire.ApiService/Program.cs` using `app.MapGet/MapPost`. No controller classes are used.
- Do not add boilerplate weatherforecast endpoint when adding new endpoints.


 
### Typed HTTP clients
- API communication from the frontend uses typed `HttpClient` wrappers (see `WeatherApiClient.cs`). Use `GetFromJsonAsAsyncEnumerable` for streaming collections. Register with `builder.Services.AddHttpClient<T>`.


### Blazor render modes
- Use interactive Blazor Server components only where needed. Default to static SSR for maximum performance and simplicity.
- Interactive UI: `@rendermode InteractiveServer` (see `Counter.razor`)
- Async data load with progressive rendering: `@attribute [StreamRendering(true)]` (see `Weather.razor`)
- Output caching via Redis: `@attribute [OutputCache(Duration = 5)]` (requires `builder.AddRedisOutputCache("cache")` in `Web/Program.cs`)

- 
## Testing
- Create integration tests in the `FarmAppAspire.Tests` project using xUnit v3 and `Aspire.Hosting.Testing`. This allows you to test the full Aspire app end-to-end, including real HTTP interactions, service discovery, and resilience behavior.
- Generate test data using the `TestDataGenerator` class in the `Tests` project. This ensures consistent, realistic test data across all tests.
- Use `Aspire.Hosting.Testing` to spin up the full Aspire app in-process for integration tests. This allows you to test real HTTP interactions, service discovery, and resilience behavior.
- Tests use `DistributedApplicationTestingBuilder` to spin up the **full Aspire app** in-process:
```csharp
var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.FarmAppAspire_AppHost>(cancellationToken);
await using var app = await appHost.BuildAsync(cancellationToken);
await app.StartAsync(cancellationToken);
var httpClient = app.CreateHttpClient("webfrontend");
await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken);
```
- Always `WaitForResourceHealthyAsync` before making HTTP assertions. Use `TestContext.Current.CancellationToken` for xUnit v3 cancellation.
- Run tests:
```
dotnet test FarmAppAspire.Tests
```
- Always run tests via the test project, not individual test files or methods, to ensure the full Aspire app is running.
- Use the Aspire dashboard to debug test failures and inspect resource health, logs, and telemetry.
- Tests are designed to be fully parallelizable. Avoid shared state between tests to prevent interference.


## Solution Format

The solution uses `.slnx` (XML-based, `FarmAppAspire.slnx`) instead of the legacy `.sln` format. Use `dotnet sln` commands with the `.slnx` file.

## Key Files
- `FarmAppAspire.AppHost/AppHost.cs` – resource graph, dependencies, health check paths
- `FarmAppAspire.ServiceDefaults/Extensions.cs` – shared OTel/resilience/health config
- `FarmAppAspire.Web/WeatherApiClient.cs` – reference pattern for typed API clients
- `FarmAppAspire.Web/Components/Pages/Weather.razor` – reference pattern for streaming + cached data pages
