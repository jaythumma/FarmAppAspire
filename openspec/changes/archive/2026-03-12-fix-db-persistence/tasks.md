## 1. Add Persistent Data Volumes in AppHost

- [x] 1.1 In `FarmAppAspire.AppHost/AppHost.cs`, chain `.WithDataVolume()` onto the `identity-db` Postgres resource declaration
- [x] 1.2 In `FarmAppAspire.AppHost/AppHost.cs`, chain `.WithDataVolume()` onto the `customer-db` Postgres resource declaration

## 2. Verification

- [x] 2.1 Run `dotnet build` — confirm zero errors
- [x] 2.2 Run `dotnet run --project FarmAppAspire.AppHost` — confirm both databases start without the `__EFMigrationsHistory` error on subsequent restarts
- [x] 2.3 Create a customer record via the UI, stop the app (`Ctrl+C`), restart it, and confirm the customer record is still present
- [x] 2.4 Run `dotnet test FarmAppAspire.Tests.Unit` — all pass (no changes expected)
- [x] 2.5 Run `dotnet test FarmAppAspire.Tests` — all pass (Testcontainers are unaffected)
