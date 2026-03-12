## 1. Project Scaffolding & Solution Setup

- [x] 1.1 Create `FarmAppAspire.CustomerService` project: `dotnet new web -n FarmAppAspire.CustomerService -o FarmAppAspire.CustomerService --framework net10.0`.
- [x] 1.2 Add the new project to `FarmAppAspire.slnx`: `dotnet sln FarmAppAspire.slnx add FarmAppAspire.CustomerService/FarmAppAspire.CustomerService.csproj`.
- [x] 1.3 Add a `ProjectReference` to `FarmAppAspire.ServiceDefaults` in `FarmAppAspire.CustomerService.csproj`.
- [x] 1.4 Add NuGet packages to `FarmAppAspire.CustomerService.csproj`: `Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.EntityFrameworkCore.Design`.

## 2. AppHost Wiring

- [x] 2.1 In `AppHost.cs`, add `var customerDb = builder.AddPostgres("customer-db")`.
- [x] 2.2 Add `var customerService = builder.AddProject<Projects.FarmAppAspire_CustomerService>("customerservice").WithReference(customerDb).WaitFor(customerDb)`.
- [x] 2.3 Chain `.WithReference(customerService).WaitFor(customerService)` onto the existing `webfrontend` builder entry.

## 3. CustomerService — Domain Entities

- [x] 3.1 Create `FarmAppAspire.CustomerService/Models/CustomerType.cs` — enum `Wholesale | Retail`.
- [x] 3.2 Create `FarmAppAspire.CustomerService/Models/ContactRole.cs` — enum `Primary | Billing | Purchasing | Shipping | Secondary | Other`.
- [x] 3.3 Create `FarmAppAspire.CustomerService/Models/AddressType.cs` — enum `Billing | Shipping | Both`.
- [x] 3.4 Create `FarmAppAspire.CustomerService/Models/PaymentTerms.cs` — enum `NET30 | NET60 | COD | Prepaid | Other`.
- [x] 3.5 Create `FarmAppAspire.CustomerService/Models/Customer.cs` — properties: `Id` (Guid), `Type`, `DisplayName`, `PrimaryEmail?`, `PrimaryPhone?`, `CompanyName?`, `TaxId?`, `PaymentTerms?`, `Notes?`, `CreatedAt`, `CreatedBy` (string), `ModifiedAt?`, `ModifiedBy?`, navigation collections `Contacts` and `Addresses`.
- [x] 3.6 Create `FarmAppAspire.CustomerService/Models/CustomerContact.cs` — properties: `Id` (Guid), `CustomerId` (Guid FK), `Role`, `FirstName`, `LastName`, `Email?`, `Phone?`, `Mobile?`, `IsPrimary`.
- [x] 3.7 Create `FarmAppAspire.CustomerService/Models/CustomerAddress.cs` — properties: `Id` (Guid), `CustomerId` (Guid FK), `Label`, `Type`, `Line1`, `Line2?`, `City`, `State`, `PostalCode`, `Country`, `IsDefault`, `CreatedAt`.

## 4. CustomerService — DbContext & Migration

- [x] 4.1 Create `FarmAppAspire.CustomerService/Data/CustomerDbContext.cs` with `DbSet<Customer>`, `DbSet<CustomerContact>`, `DbSet<CustomerAddress>`; configure cascade deletes and the `Type` discriminator using `HasDiscriminator` or a `TPH` value converter.
- [x] 4.2 In `CustomerService/Program.cs`, call `builder.AddServiceDefaults()`, then `builder.AddNpgsqlDbContext<CustomerDbContext>("customer-db")`.
- [x] 4.3 Run `dotnet ef migrations add InitialCustomerSchema --project FarmAppAspire.CustomerService` to generate the initial migration.
- [x] 4.4 Add startup migration call: `app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope()...MigrateAsync()` (or an `IHostedService`) before `app.Run()`.

## 5. CustomerService — Minimal API Endpoints

- [x] 5.1 Implement `GET /customers` — paginated list (`?page=1&size=25`), reads `X-User-Id` header, returns `IEnumerable<CustomerSummaryDto>`.
- [x] 5.2 Implement `GET /customers/{id}` — returns full customer with contacts and addresses as `CustomerDetailDto`; HTTP 404 if not found.
- [x] 5.3 Implement `POST /customers` — validates required fields, reads `X-User-Id` for `CreatedBy`, returns HTTP 201 with the created `CustomerDetailDto`.
- [x] 5.4 Implement `PUT /customers/{id}` — updates customer fields, sets `ModifiedBy` from `X-User-Id`, returns updated `CustomerDetailDto`.
- [x] 5.5 Implement `DELETE /customers/{id}` — removes customer and cascaded contacts/addresses; returns HTTP 204.
- [x] 5.6 Implement `GET /customers/{id}/contacts`, `POST /customers/{id}/contacts`, `PUT /customers/{id}/contacts/{cid}`, `DELETE /customers/{id}/contacts/{cid}`.
- [x] 5.7 Implement `GET /customers/{id}/addresses`, `POST /customers/{id}/addresses`, `PUT /customers/{id}/addresses/{aid}`, `DELETE /customers/{id}/addresses/{aid}` — including default-address promotion logic on delete.
- [x] 5.8 Add a middleware filter or endpoint filter that validates the `X-User-Id` header is present on all `POST`, `PUT`, and `DELETE` endpoints; return HTTP 400 if missing.
- [x] 5.9 Enable OpenAPI (`builder.Services.AddOpenApi()`) and map it in development (`app.MapOpenApi()`).

## 6. Web Frontend — CustomerApiClient & Header Propagation

- [x] 6.1 Create `FarmAppAspire.Web/CustomerApiClientHandler.cs` — a `DelegatingHandler` that reads `ClaimTypes.NameIdentifier` from `IHttpContextAccessor` and sets the `X-User-Id` request header.
- [x] 6.2 Register `IHttpContextAccessor` in `Program.cs`: `builder.Services.AddHttpContextAccessor()`.
- [x] 6.3 Create `FarmAppAspire.Web/CustomerApiClient.cs` — typed `HttpClient` wrapping GET/POST/PUT/DELETE for customers, contacts, and addresses; deserializes JSON responses.
- [x] 6.4 Register `CustomerApiClientHandler` and `CustomerApiClient` in `Program.cs`: `builder.Services.AddTransient<CustomerApiClientHandler>()` and `builder.Services.AddHttpClient<CustomerApiClient>(client => client.BaseAddress = new("https+http://customerservice")).AddHttpMessageHandler<CustomerApiClientHandler>()`.

## 7. Web Frontend — Blazor Customer Pages

- [x] 7.1 Create `FarmAppAspire.Web/Components/Pages/Customers/List.razor` at route `/customers` — `@rendermode InteractiveServer`, `@attribute [Authorize]`; show paginated table with search and type filter; hide create/edit actions for `ReadOnly` and `Staff`.
- [x] 7.2 Create `FarmAppAspire.Web/Components/Pages/Customers/Detail.razor` at route `/customers/{Id:guid}` — show all customer fields, contacts table with roles, addresses table with type and default indicator.
- [x] 7.3 Create `FarmAppAspire.Web/Components/Pages/Customers/CreateEdit.razor` at route `/customers/create` and `/customers/{Id:guid}/edit` — `@attribute [Authorize(Roles = "FarmAdmin,Manager")]`; show/hide wholesale-only fields based on selected type.
- [x] 7.4 Add a "Customers" nav link in `NavMenu.razor` (visible to all authenticated users) inside an `<AuthorizeView>` block.

## 8. Verification

- [ ] 8.1 Run `dotnet run --project FarmAppAspire.AppHost`; confirm `customer-db` container starts and `CustomerService` migration runs.
- [ ] 8.2 Use the Aspire dashboard to confirm `customerservice` is healthy.
- [ ] 8.3 Log in as a Manager; create one Wholesale and one Retail customer via the UI.
- [ ] 8.4 Add contacts with different roles and multiple addresses to each customer; confirm default address promotion works on delete.
- [ ] 8.5 Log in as Staff; confirm the customer list is visible but create/edit controls are hidden.
- [ ] 8.6 Log in as ReadOnly; confirm no create or edit controls are present anywhere in the customer section.
- [ ] 8.7 Inspect the `customers` table in `customer-db`; confirm `CreatedBy` is populated with the logged-in user's Identity GUID.
- [ ] 8.8 Run `dotnet test FarmAppAspire.Tests`; confirm existing integration tests still pass.
