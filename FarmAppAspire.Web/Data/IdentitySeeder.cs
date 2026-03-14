using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FarmAppAspire.Web.Data;

public class IdentitySeeder(IServiceScopeFactory scopeFactory, ILogger<IdentitySeeder> logger) : IHostedService
{
    public static readonly string[] Roles = ["FarmAdmin", "Manager", "Staff", "Customer"];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = config["FarmAdmin:Email"];
        var password = config["FarmAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("FarmAdmin:Email or FarmAdmin:Password not configured; skipping admin seed.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await userManager.ResetPasswordAsync(existing, token, password);
            if (resetResult.Succeeded)
                logger.LogInformation("Admin user {Email} password updated.", email);
            else
                logger.LogError("Failed to reset admin password: {Errors}",
                    string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            return;
        }

        if (await userManager.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Users already exist; skipping admin seed.");
            return;
        }

        var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, IsActive = true };
        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "FarmAdmin");
            logger.LogInformation("Admin user {Email} seeded.", email);
        }
        else
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
