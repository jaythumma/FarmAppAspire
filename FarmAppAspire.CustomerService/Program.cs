using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Endpoints;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.AddNpgsqlDbContext<CustomerDbContext>("customer-db");
builder.Services.AddSingleton<IAddressValidationService, FormatAddressValidationService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

// ── Startup migration ────────────────────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CustomerService.Migrations");

    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (!pending.Any())
    {
        logger.LogInformation("No pending EF Core migrations for CustomerDbContext.");
    }
    else
    {
        try
        {
            // If the database already contains the Customers table (created outside of migrations)
            // attempting to apply migrations will fail with "relation already exists". Detect that
            // case and skip applying migrations to avoid conflicts in development environments.
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            await using (var cmd = conn.CreateCommand())
            {
                // Use case-insensitive search for the customers table via pg_class to avoid
                // issues with quoted identifiers (EF may generate quoted names). This
                // works whether the table was created with or without double quotes.
                cmd.CommandText =
                    "SELECT EXISTS (" +
                    "  SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace" +
                    "  WHERE n.nspname = 'public' AND lower(c.relname) = lower(@name) AND c.relkind = 'r'" +
                    ")";
                var p = cmd.CreateParameter();
                p.ParameterName = "@name";
                p.Value = "Customers";
                cmd.Parameters.Add(p);
                var existsObj = await cmd.ExecuteScalarAsync();
                var exists = existsObj is bool b && b;
                if (!exists)
                {
                    logger.LogInformation("Applying {Count} pending EF Core migrations for CustomerDbContext.", pending.Count);
                    await db.Database.MigrateAsync();
                }
                else
                {
                    // If the schema exists but the migrations history is empty (database created outside EF),
                    // mark pending migrations as applied in __EFMigrationsHistory so EF won't attempt to run them.
                    logger.LogWarning("Detected existing schema (Customers table present) but migrations are pending.");

                    await using (var histCmd = conn.CreateCommand())
                    {
                        histCmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"";
                        try
                        {
                            var cntObj = await histCmd.ExecuteScalarAsync();
                            var cnt = Convert.ToInt32(cntObj);
                            if (cnt == 0)
                            {
                                logger.LogInformation("__EFMigrationsHistory is empty but schema exists. Recording pending migrations as applied to avoid duplicate creation.");
                                foreach (var m in pending)
                                {
                                    await using var ins = conn.CreateCommand();
                                    ins.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@mid, @pv)";
                                    var pm = ins.CreateParameter(); pm.ParameterName = "@mid"; pm.Value = m; ins.Parameters.Add(pm);
                                    var pp = ins.CreateParameter(); pp.ParameterName = "@pv"; pp.Value = "10.0.4"; ins.Parameters.Add(pp);
                                    await ins.ExecuteNonQueryAsync();
                                }
                                logger.LogInformation("Recorded {Count} pending migrations as applied.", pending.Count);
                            }
                            else
                            {
                                logger.LogWarning("__EFMigrationsHistory already contains entries ({Count}); skipping automatic migrate.", cnt);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to inspect or update __EFMigrationsHistory; skipping automatic migration to avoid conflicts.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // If detection fails, attempt to apply migrations (best-effort) but log the error.
            var lf = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CustomerService.Migrations");
            lf.LogWarning(ex, "Failed to detect existing schema; attempting to apply migrations as fallback.");
            await db.Database.MigrateAsync();
        }
    }
}

// ── X-User-Id filter ─────────────────────────────────────────────────────────
EndpointFilterDelegate UserIdFilter(EndpointFilterFactoryContext _, EndpointFilterDelegate next)
    => async ctx =>
    {
        if (!ctx.HttpContext.Request.Headers.TryGetValue("X-User-Id", out var id) || string.IsNullOrWhiteSpace(id))
            return Results.Problem("X-User-Id header is required.", statusCode: StatusCodes.Status400BadRequest);
        return await next(ctx);
    };

// ── Route groups ──────────────────────────────────────────────────────────────
var customers = app.MapGroup("/customers").WithTags("Customers");

// GET /customers
customers.MapGet("", async (CustomerDbContext db, int page = 1, int size = 25) =>
{
    var total = await db.Customers.CountAsync();
    var customerPage = await db.Customers
        .OrderBy(c => c.DisplayName)
        .Skip((page - 1) * size).Take(size)
        .ToListAsync();

    var customerIds = customerPage.Select(c => c.Id).ToList();
    var boxConfigs  = await db.InsulatedBoxConfigs.ToListAsync();

    var orders = await db.Orders
        .Include(o => o.Lines)
        .Where(o => customerIds.Contains(o.CustomerId) && o.Status != OrderStatus.Cancelled)
        .ToListAsync();

    var ordersByCustomer = orders
        .GroupBy(o => o.CustomerId)
        .ToDictionary(g => g.Key, g => g.ToList());

    var items = customerPage.Select(c =>
    {
        var customerOrders = ordersByCustomer.GetValueOrDefault(c.Id, []);
        var orderCount    = customerOrders.Count;
        var totalAmount   = customerOrders.Sum(o =>
            PriceResolutionService.ComputeOrderTotals(o.Lines, boxConfigs).TotalAmount);
        return c.ToSummaryDto(orderCount, totalAmount);
    }).ToList();

    return Results.Ok(new { total, page, size, items });
});

// GET /customers/{id}
customers.MapGet("{id:guid}", async (Guid id, CustomerDbContext db) =>
{
    var c = await db.Customers
        .Include(x => x.Contacts)
        .Include(x => x.Addresses)
        .FirstOrDefaultAsync(x => x.Id == id);
    if (c is null) return Results.NotFound();

    var boxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
    var orders     = await db.Orders
        .Include(o => o.Lines)
        .Where(o => o.CustomerId == id && o.Status != OrderStatus.Cancelled)
        .ToListAsync();

    var orderCount  = orders.Count;
    var totalAmount = orders.Sum(o =>
        PriceResolutionService.ComputeOrderTotals(o.Lines, boxConfigs).TotalAmount);

    return Results.Ok(c.ToDetailDto(orderCount, totalAmount));
});

// POST /customers
customers.MapPost("", async (CreateCustomerRequest req, CustomerDbContext db, HttpContext ctx, IAddressValidationService addressValidator) =>
{
    if (string.IsNullOrWhiteSpace(req.DisplayName))
        return Results.Problem("DisplayName is required.", statusCode: StatusCodes.Status400BadRequest);
    if (req.Type == CustomerType.Wholesale && string.IsNullOrWhiteSpace(req.CompanyName))
        return Results.Problem("CompanyName is required for Wholesale customers.", statusCode: StatusCodes.Status400BadRequest);

    if (string.IsNullOrWhiteSpace(req.ShippingAddress?.Line1) ||
        string.IsNullOrWhiteSpace(req.ShippingAddress?.City) ||
        string.IsNullOrWhiteSpace(req.ShippingAddress?.State) ||
        string.IsNullOrWhiteSpace(req.ShippingAddress?.PostalCode) ||
        string.IsNullOrWhiteSpace(req.ShippingAddress?.Country))
        return Results.Problem("Shipping address with Line1, City, State, PostalCode, and Country is required.", statusCode: StatusCodes.Status400BadRequest);

    var shippingValidation = await addressValidator.ValidateAsync(
        req.ShippingAddress.Line1, req.ShippingAddress.Line2,
        req.ShippingAddress.City, req.ShippingAddress.State,
        req.ShippingAddress.PostalCode, req.ShippingAddress.Country,
        ctx.RequestAborted);
    if (!shippingValidation.IsValid)
        return Results.Problem(string.Join(" ", shippingValidation.Errors), title: "Address not valid", statusCode: StatusCodes.Status422UnprocessableEntity);

    if (!req.BillingUsesShipping && req.BillingAddress is not null)
    {
        var billingValidation = await addressValidator.ValidateAsync(
            req.BillingAddress.Line1, req.BillingAddress.Line2,
            req.BillingAddress.City, req.BillingAddress.State,
            req.BillingAddress.PostalCode, req.BillingAddress.Country,
            ctx.RequestAborted);
        if (!billingValidation.IsValid)
            return Results.Problem(string.Join(" ", billingValidation.Errors), title: "Address not valid", statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var now = DateTime.UtcNow;
    var userId = ctx.Request.Headers["X-User-Id"].ToString();

    var customer = new Customer
    {
        Id = Guid.NewGuid(),
        Type = req.Type,
        ChannelType = req.ChannelType,
        DisplayName = req.DisplayName,
        CompanyName = req.CompanyName,
        TaxId = req.TaxId,
        PaymentTerms = req.PaymentTerms,
        PrimaryEmail = req.PrimaryEmail,
        PrimaryPhone = req.PrimaryPhone,
        Notes = req.Notes,
        BillingUsesShipping = req.BillingUsesShipping,
        CreatedAt = now,
        CreatedBy = userId,
    };
    db.Customers.Add(customer);

    db.CustomerAddresses.Add(new CustomerAddress
    {
        Id = Guid.NewGuid(),
        CustomerId = customer.Id,
        Label = "Shipping",
        Type = AddressType.Shipping,
        Line1 = req.ShippingAddress.Line1,
        Line2 = req.ShippingAddress.Line2,
        City = req.ShippingAddress.City,
        State = req.ShippingAddress.State,
        PostalCode = req.ShippingAddress.PostalCode,
        Country = req.ShippingAddress.Country,
        IsDefault = true,
        CreatedAt = now,
    });

    if (!req.BillingUsesShipping && req.BillingAddress is not null)
    {
        db.CustomerAddresses.Add(new CustomerAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Label = "Billing",
            Type = AddressType.Billing,
            Line1 = req.BillingAddress.Line1,
            Line2 = req.BillingAddress.Line2,
            City = req.BillingAddress.City,
            State = req.BillingAddress.State,
            PostalCode = req.BillingAddress.PostalCode,
            Country = req.BillingAddress.Country,
            IsDefault = false,
            CreatedAt = now,
        });
    }

    // Auto-generate CustomerKey for Amazon channel customers
    if (req.ChannelType == ChannelType.Amazon)
    {
        var svc = new CustomerKeyService();
        var keyTarget = new Customer
        {
            Id = customer.Id,
            DisplayName = customer.DisplayName,
            ChannelType = ChannelType.Amazon,
            Addresses =
            [
                new CustomerAddress
                {
                    Type = AddressType.Shipping,
                    IsDefault = true,
                    City = req.ShippingAddress.City,
                    State = req.ShippingAddress.State,
                    Line1 = req.ShippingAddress.Line1,
                    PostalCode = req.ShippingAddress.PostalCode,
                    Country = req.ShippingAddress.Country,
                    CreatedAt = now,
                }
            ]
        };
        var proposed = svc.Generate(keyTarget);
        var existingKeys = await db.Customers
            .Where(c => c.CustomerKey != null)
            .Select(c => new { c.CustomerKey, c.Id })
            .ToListAsync();
        customer.CustomerKey = proposed;
        customer.CustomerKeyCollision = CustomerKeyService.CheckCollision(
            proposed, customer.Id, existingKeys.Select(e => (e.CustomerKey!, e.Id)));
    }

    await db.SaveChangesAsync();

    var created = await db.Customers
        .Include(x => x.Contacts)
        .Include(x => x.Addresses)
        .FirstAsync(x => x.Id == customer.Id);
    return Results.Created($"/customers/{customer.Id}", created.ToDetailDto(0, 0m));
}).AddEndpointFilterFactory(UserIdFilter);

// PUT /customers/{id}
customers.MapPut("{id:guid}", async (Guid id, UpdateCustomerRequest req, CustomerDbContext db, HttpContext ctx) =>
{
    var c = await db.Customers.FindAsync(id);
    if (c is null) return Results.NotFound();
    if (c.Type == CustomerType.Wholesale && string.IsNullOrWhiteSpace(req.CompanyName))
        return Results.Problem("CompanyName is required for Wholesale customers.", statusCode: StatusCodes.Status400BadRequest);
    c.DisplayName = req.DisplayName;
    c.CompanyName = req.CompanyName;
    c.TaxId = req.TaxId;
    c.PaymentTerms = req.PaymentTerms;
    c.PrimaryEmail = req.PrimaryEmail;
    c.PrimaryPhone = req.PrimaryPhone;
    c.Notes = req.Notes;
    c.ChannelType = req.ChannelType;
    if (req.BillingUsesShipping.HasValue)
        c.BillingUsesShipping = req.BillingUsesShipping.Value;
    c.ModifiedAt = DateTime.UtcNow;
    c.ModifiedBy = ctx.Request.Headers["X-User-Id"].ToString();

    // Auto-regenerate CustomerKey for Amazon channel customers
    if (req.ChannelType == ChannelType.Amazon)
    {
        await db.CustomerAddresses.Where(a => a.CustomerId == id).LoadAsync();
        var svc = new CustomerKeyService();
        var proposed = svc.Generate(c);
        var existingKeys = await db.Customers
            .Where(x => x.CustomerKey != null && x.Id != id)
            .Select(x => new { x.CustomerKey, x.Id })
            .ToListAsync();
        c.CustomerKey = proposed;
        c.CustomerKeyCollision = CustomerKeyService.CheckCollision(
            proposed, id, existingKeys.Select(e => (e.CustomerKey!, e.Id)));
    }

    await db.SaveChangesAsync();
    var full = await db.Customers.Include(x => x.Contacts).Include(x => x.Addresses).FirstAsync(x => x.Id == id);
    var putBoxConfigs = await db.InsulatedBoxConfigs.ToListAsync();
    var putOrders     = await db.Orders
        .Include(o => o.Lines)
        .Where(o => o.CustomerId == id && o.Status != OrderStatus.Cancelled)
        .ToListAsync();
    var putOrderCount  = putOrders.Count;
    var putTotalAmount = putOrders.Sum(o =>
        PriceResolutionService.ComputeOrderTotals(o.Lines, putBoxConfigs).TotalAmount);
    return Results.Ok(full.ToDetailDto(putOrderCount, putTotalAmount));
}).AddEndpointFilterFactory(UserIdFilter);

// DELETE /customers/{id}
customers.MapDelete("{id:guid}", async (Guid id, CustomerDbContext db, HttpContext ctx) =>
{
    var c = await db.Customers.FindAsync(id);
    if (c is null) return Results.NotFound();
    db.Customers.Remove(c);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).AddEndpointFilterFactory(UserIdFilter);

// ── Contacts ──────────────────────────────────────────────────────────────────
customers.MapGet("{id:guid}/contacts", async (Guid id, CustomerDbContext db) =>
{
    if (!await db.Customers.AnyAsync(c => c.Id == id)) return Results.NotFound();
    var contacts = await db.CustomerContacts
        .Where(c => c.CustomerId == id)
        .OrderByDescending(c => c.IsPrimary)
        .Select(c => c.ToDto())
        .ToListAsync();
    return Results.Ok(contacts);
});

customers.MapPost("{id:guid}/contacts", async (Guid id, CreateContactRequest req, CustomerDbContext db, HttpContext ctx) =>
{
    if (!await db.Customers.AnyAsync(c => c.Id == id)) return Results.NotFound();
    var contact = new CustomerContact
    {
        Id = Guid.NewGuid(),
        CustomerId = id,
        Role = req.Role,
        FirstName = req.FirstName,
        LastName = req.LastName,
        Email = req.Email,
        Phone = req.Phone,
        Mobile = req.Mobile,
        IsPrimary = req.IsPrimary,
    };
    db.CustomerContacts.Add(contact);
    await db.SaveChangesAsync();
    return Results.Created($"/customers/{id}/contacts/{contact.Id}", contact.ToDto());
}).AddEndpointFilterFactory(UserIdFilter);

customers.MapPut("{id:guid}/contacts/{cid:guid}", async (Guid id, Guid cid, UpdateContactRequest req, CustomerDbContext db, HttpContext ctx) =>
{
    var contact = await db.CustomerContacts.FirstOrDefaultAsync(c => c.Id == cid && c.CustomerId == id);
    if (contact is null) return Results.NotFound();
    contact.Role = req.Role;
    contact.FirstName = req.FirstName;
    contact.LastName = req.LastName;
    contact.Email = req.Email;
    contact.Phone = req.Phone;
    contact.Mobile = req.Mobile;
    contact.IsPrimary = req.IsPrimary;
    await db.SaveChangesAsync();
    return Results.Ok(contact.ToDto());
}).AddEndpointFilterFactory(UserIdFilter);

customers.MapDelete("{id:guid}/contacts/{cid:guid}", async (Guid id, Guid cid, CustomerDbContext db, HttpContext ctx) =>
{
    var contact = await db.CustomerContacts.FirstOrDefaultAsync(c => c.Id == cid && c.CustomerId == id);
    if (contact is null) return Results.NotFound();
    db.CustomerContacts.Remove(contact);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).AddEndpointFilterFactory(UserIdFilter);

// ── Addresses ─────────────────────────────────────────────────────────────────
customers.MapGet("{id:guid}/addresses", async (Guid id, CustomerDbContext db) =>
{
    if (!await db.Customers.AnyAsync(c => c.Id == id)) return Results.NotFound();
    var addresses = await db.CustomerAddresses
        .Where(a => a.CustomerId == id)
        .OrderByDescending(a => a.IsDefault)
        .ThenBy(a => a.CreatedAt)
        .Select(a => a.ToDto())
        .ToListAsync();
    return Results.Ok(addresses);
});

customers.MapPost("{id:guid}/addresses", async (Guid id, CreateAddressRequest req, CustomerDbContext db, HttpContext ctx, IAddressValidationService addressValidator) =>
{
    if (!await db.Customers.AnyAsync(c => c.Id == id)) return Results.NotFound();

    var validation = await addressValidator.ValidateAsync(
        req.Line1, req.Line2, req.City, req.State, req.PostalCode, req.Country,
        ctx.RequestAborted);
    if (!validation.IsValid)
        return Results.Problem(string.Join(" ", validation.Errors), title: "Address not valid", statusCode: StatusCodes.Status422UnprocessableEntity);

    var isFirst = !await db.CustomerAddresses.AnyAsync(a => a.CustomerId == id);
    if (req.IsDefault || isFirst)
        await db.CustomerAddresses.Where(a => a.CustomerId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
    var address = new CustomerAddress
    {
        Id = Guid.NewGuid(),
        CustomerId = id,
        Label = req.Label,
        Type = req.Type,
        Line1 = req.Line1,
        Line2 = req.Line2,
        City = req.City,
        State = req.State,
        PostalCode = req.PostalCode,
        Country = req.Country,
        IsDefault = req.IsDefault || isFirst,
        CreatedAt = DateTime.UtcNow,
    };
    db.CustomerAddresses.Add(address);
    await db.SaveChangesAsync();
    return Results.Created($"/customers/{id}/addresses/{address.Id}", address.ToDto());
}).AddEndpointFilterFactory(UserIdFilter);

customers.MapPut("{id:guid}/addresses/{aid:guid}", async (Guid id, Guid aid, UpdateAddressRequest req, CustomerDbContext db, HttpContext ctx, IAddressValidationService addressValidator) =>
{
    var address = await db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == aid && a.CustomerId == id);
    if (address is null) return Results.NotFound();

    var validation = await addressValidator.ValidateAsync(
        req.Line1, req.Line2, req.City, req.State, req.PostalCode, req.Country,
        ctx.RequestAborted);
    if (!validation.IsValid)
        return Results.Problem(string.Join(" ", validation.Errors), title: "Address not valid", statusCode: StatusCodes.Status422UnprocessableEntity);

    if (req.IsDefault && !address.IsDefault)
        await db.CustomerAddresses.Where(a => a.CustomerId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
    address.Label = req.Label;
    address.Type = req.Type;
    address.Line1 = req.Line1;
    address.Line2 = req.Line2;
    address.City = req.City;
    address.State = req.State;
    address.PostalCode = req.PostalCode;
    address.Country = req.Country;
    address.IsDefault = req.IsDefault;

    if (req.Type is AddressType.Shipping or AddressType.Both)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is not null && customer.BillingUsesShipping)
        {
            customer.BillingUsesShipping = false;
        }
    }

    await db.SaveChangesAsync();
    return Results.Ok(address.ToDto());
}).AddEndpointFilterFactory(UserIdFilter);

customers.MapDelete("{id:guid}/addresses/{aid:guid}", async (Guid id, Guid aid, CustomerDbContext db, HttpContext ctx) =>
{
    var address = await db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == aid && a.CustomerId == id);
    if (address is null) return Results.NotFound();
    var wasDefault = address.IsDefault;
    db.CustomerAddresses.Remove(address);
    await db.SaveChangesAsync();
    if (wasDefault)
    {
        var next = await db.CustomerAddresses.Where(a => a.CustomerId == id).OrderBy(a => a.CreatedAt).FirstOrDefaultAsync();
        if (next is not null) { next.IsDefault = true; await db.SaveChangesAsync(); }
    }
    return Results.NoContent();
}).AddEndpointFilterFactory(UserIdFilter);

app.MapDefaultEndpoints();

// ── Order management routes ──────────────────────────────────────────────────
app.MapProductEndpoints();
app.MapCustomerPricingEndpoints();
app.MapStandingOrderEndpoints();
app.MapOrderEndpoints();
app.MapInvoiceEndpoints();
app.MapCustomerKeyEndpoints();
app.MapAdminEndpoints();

app.Run();
