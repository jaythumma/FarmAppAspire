using FarmAppAspire.Web.Data;
using FarmAppAspire.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FarmAppAspire.Tests.Unit.Auth;

public sealed class UserAdminServiceTests : IAsyncDisposable
{
    private const string TestPassword = "Password123";

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _sp;

    public UserAdminServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _sp = BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProvider()
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

        services.AddScoped<UserAdminService>();
        return services.BuildServiceProvider();
    }

    private async Task SeedSchemaAndRolesAsync()
    {
        await using var scope = _sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in IdentitySeeder.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string role)
    {
        await using var scope = _sp.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, IsActive = true };
        var result = await userManager.CreateAsync(user, TestPassword);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private UserAdminService GetService(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<UserAdminService>();

    [Fact]
    public async Task CreateUser_Succeeds_WithValidData()
    {
        await SeedSchemaAndRolesAsync();
        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var (success, error) = await svc.CreateUserAsync("new@test.com", "New User", "Staff", TestPassword);

        Assert.True(success);
        Assert.Null(error);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("new@test.com");
        Assert.NotNull(user);
        Assert.Equal("New User", user.DisplayName);
        Assert.True(user.IsActive);
        var roles = await userManager.GetRolesAsync(user);
        Assert.Contains("Staff", roles);
    }

    [Fact]
    public async Task CreateUser_Fails_WhenEmailAlreadyExists()
    {
        await SeedSchemaAndRolesAsync();
        await CreateUserAsync("dup@test.com", "Staff");

        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var (success, error) = await svc.CreateUserAsync("dup@test.com", "Another", "Manager", "Password456");

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateUserRole_Succeeds_ForOtherUser()
    {
        await SeedSchemaAndRolesAsync();
        var target = await CreateUserAsync("target@test.com", "Staff");
        var admin = await CreateUserAsync("admin@test.com", "FarmAdmin");

        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var (success, error) = await svc.UpdateUserRoleAsync(target.Id, "Manager", admin.Id);

        Assert.True(success);
        Assert.Null(error);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshed = await userManager.FindByIdAsync(target.Id);
        var roles = await userManager.GetRolesAsync(refreshed!);
        Assert.Contains("Manager", roles);
        Assert.DoesNotContain("Staff", roles);
    }

    [Fact]
    public async Task UpdateUserRole_Fails_WhenChangingOwnRole()
    {
        await SeedSchemaAndRolesAsync();
        var admin = await CreateUserAsync("self@test.com", "FarmAdmin");

        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var (success, error) = await svc.UpdateUserRoleAsync(admin.Id, "Staff", admin.Id);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task DeactivateUser_Succeeds_ForOtherUser()
    {
        await SeedSchemaAndRolesAsync();
        var target = await CreateUserAsync("active@test.com", "Staff");
        var admin = await CreateUserAsync("admin2@test.com", "FarmAdmin");

        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var (success, error) = await svc.DeactivateUserAsync(target.Id, admin.Id);

        Assert.True(success);
        Assert.Null(error);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshed = await userManager.FindByIdAsync(target.Id);
        Assert.False(refreshed!.IsActive);
    }

    [Fact]
    public async Task DeactivateUser_Fails_WhenDeactivatingSelf()
    {
        await SeedSchemaAndRolesAsync();
        var admin = await CreateUserAsync("selfdeact@test.com", "FarmAdmin");

        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var (success, error) = await svc.DeactivateUserAsync(admin.Id, admin.Id);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ResetPassword_Succeeds_AndReturnsTempPassword()
    {
        await SeedSchemaAndRolesAsync();
        var target = await CreateUserAsync("reset@test.com", "Staff");

        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var (success, tempPassword, error) = await svc.ResetPasswordAsync(target.Id);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotEmpty(tempPassword);
        Assert.True(tempPassword.Length >= 8, $"Temp password '{tempPassword}' is too short.");

        // Verify the temp password actually works
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshed = await userManager.FindByIdAsync(target.Id);
        var checkResult = await userManager.CheckPasswordAsync(refreshed!, tempPassword);
        Assert.True(checkResult);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsersWithRoles()
    {
        await SeedSchemaAndRolesAsync();
        await CreateUserAsync("u1@test.com", "Staff");
        await CreateUserAsync("u2@test.com", "Manager");

        await using var scope = _sp.CreateAsyncScope();
        var svc = GetService(scope);

        var users = await svc.GetAllUsersAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Email == "u1@test.com" && u.Role == "Staff");
        Assert.Contains(users, u => u.Email == "u2@test.com" && u.Role == "Manager");
        Assert.All(users, u => Assert.True(u.IsActive));
    }

    public async ValueTask DisposeAsync()
    {
        await _sp.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
