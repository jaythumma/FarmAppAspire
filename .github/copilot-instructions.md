# FarmAppAspire – Copilot Instructions

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

## Testing

Tests use `DistributedApplicationTestingBuilder` to spin up the **full Aspire app** in-process:
```csharp
var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.FarmAppAspire_AppHost>(cancellationToken);
await using var app = await appHost.BuildAsync(cancellationToken);
await app.StartAsync(cancellationToken);
var httpClient = app.CreateHttpClient("webfrontend");
await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken);
```
Always `WaitForResourceHealthyAsync` before making HTTP assertions. Use `TestContext.Current.CancellationToken` for xUnit v3 cancellation.

Run tests:
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
