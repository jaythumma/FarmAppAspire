using FarmAppAspire.Web;
using FarmAppAspire.Web.Components;
using FarmAppAspire.Web.Components.Layout;
using FarmAppAspire.Web.Data;
using FarmAppAspire.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

builder.AddNpgsqlDbContext<ApplicationDbContext>("identity-db");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHostedService<IdentitySeeder>();

builder.Services.AddScoped<UserAdminService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<CustomerApiClientHandler>();
builder.Services.AddHttpClient<CustomerApiClient>(client =>
    {
        client.BaseAddress = new("https+http://customerservice");
    })
    .AddHttpMessageHandler<CustomerApiClientHandler>();

builder.Services.AddHttpClient<OrderApiClient>(client =>
    {
        client.BaseAddress = new("https+http://customerservice");
    })
    .AddHttpMessageHandler<CustomerApiClientHandler>();

builder.Services.AddHttpClient<InvoiceApiClient>(client =>
    {
        client.BaseAddress = new("https+http://customerservice");
    })
    .AddHttpMessageHandler<CustomerApiClientHandler>();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });

var app = builder.Build();

// ── Startup migration ────────────────────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //await db.Database.MigrateAsync();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserAdminService.Migrations");

    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (!pending.Any())
    {
        logger.LogInformation("No pending EF Core migrations for ApplicationDbContext.");
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
                p.Value = "AspNetRoles";
                cmd.Parameters.Add(p);
                var existsObj = await cmd.ExecuteScalarAsync();
                var exists = existsObj is bool b && b;
                if (!exists)
                {
                    logger.LogInformation("Applying {Count} pending EF Core migrations for ApplicationDbContext.", pending.Count);
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorPages();

app.MapPost("/account/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/account/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();
