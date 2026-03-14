using FarmAppAspire.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FarmAppAspire.Tests.Unit.Auth;

public sealed class IdentitySeederTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _sp;

    public IdentitySeederTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _sp = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["FarmAdmin:Email"] = "admin@test.com",
            ["FarmAdmin:Password"] = "admin123"
        });
    }

    private ServiceProvider BuildServiceProvider(Dictionary<string, string?> configValues)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlite(_connection)
             .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
        {
            opts.Password.RequireDigit = false;
            opts.Password.RequireLowercase = false;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequiredLength = 6;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection(configValues).Build());

        return services.BuildServiceProvider();
    }

    private IdentitySeeder CreateSeeder(ServiceProvider? sp = null) =>
        new((sp ?? _sp).GetRequiredService<IServiceScopeFactory>(),
            NullLogger<IdentitySeeder>.Instance);

    [Fact]
    public async Task Seeds_FourRoles_WhenNoneExist()
    {
        var ct = TestContext.Current.CancellationToken;
        await CreateSeeder().StartAsync(ct);

        await using var scope = _sp.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in IdentitySeeder.Roles)
            Assert.True(await roleManager.RoleExistsAsync(role), $"Role '{role}' was not seeded.");
    }

    [Fact]
    public async Task Seeder_IsIdempotent_DoesNotDuplicateRoles()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeder = CreateSeeder();
        await seeder.StartAsync(ct);
        await seeder.StartAsync(ct);

        await using var scope = _sp.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = await roleManager.Roles.ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(IdentitySeeder.Roles.Length, roles.Count);
    }

    [Fact]
    public async Task Seeds_FarmAdmin_WhenNoUsersExist()
    {
        var ct = TestContext.Current.CancellationToken;
        await CreateSeeder().StartAsync(ct);

        await using var scope = _sp.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await userManager.FindByEmailAsync("admin@test.com");
        Assert.NotNull(admin);

        var roles = await userManager.GetRolesAsync(admin);
        Assert.Contains("FarmAdmin", roles);
    }

    [Fact]
    public async Task Skips_UserSeed_WhenUsersAlreadyExist()
    {
        var ct = TestContext.Current.CancellationToken;
        // Prime schema and roles by running once
        await CreateSeeder().StartAsync(ct);

        // Add a second user directly to simulate existing users
        await using var setupScope = _sp.CreateAsyncScope();
        var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var extra = new ApplicationUser { UserName = "other@test.com", Email = "other@test.com", EmailConfirmed = true };
        await userManager.CreateAsync(extra, "other123");

        // Build a fresh seeder with different admin credentials
        var altSp = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["FarmAdmin:Email"] = "newadmin@test.com",
            ["FarmAdmin:Password"] = "newadmin123"
        });

        // Run seeder again — there are already users so it should skip user creation
        await CreateSeeder(altSp).StartAsync(ct);

        await using var assertScope = _sp.CreateAsyncScope();
        var um = assertScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Null(await um.FindByEmailAsync("newadmin@test.com"));
    }

    [Fact]
    public async Task Skips_UserSeed_WhenConfigMissing()
    {
        var ct = TestContext.Current.CancellationToken;
        var sp = BuildServiceProvider(new Dictionary<string, string?>());
        await CreateSeeder(sp).StartAsync(ct);

        await using var scope = sp.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Equal(0, await userManager.Users.CountAsync(TestContext.Current.CancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        await _sp.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
