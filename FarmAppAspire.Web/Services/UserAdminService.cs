using FarmAppAspire.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Web.Services;

public record UserRow(string Id, string Email, string DisplayName, string Role, bool IsActive);

public class UserAdminService(UserManager<ApplicationUser> userManager)
{
    public async Task<List<UserRow>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await userManager.Users.ToListAsync(ct);
        var result = new List<UserRow>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserRow(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                roles.FirstOrDefault() ?? string.Empty,
                user.IsActive));
        }
        return result;
    }

    public async Task<(bool Success, string? Error)> CreateUserAsync(
        string email, string displayName, string role, string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return (false, "A user with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(role))
            await userManager.AddToRoleAsync(user, role);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateUserRoleAsync(
        string userId, string newRole, string currentUserId)
    {
        if (userId == currentUserId)
            return (false, "You cannot change your own role.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        var currentRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            return (false, string.Join("; ", removeResult.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(newRole))
        {
            var addResult = await userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
                return (false, string.Join("; ", addResult.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateUserAsync(
        string userId, string currentUserId)
    {
        if (userId == currentUserId)
            return (false, "You cannot deactivate your own account.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<(bool Success, string TempPassword, string? Error)> ResetPasswordAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, string.Empty, "User not found.");

        var tempPassword = GenerateTempPassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, tempPassword);
        if (!result.Succeeded)
            return (false, string.Empty, string.Join("; ", result.Errors.Select(e => e.Description)));

        return (true, tempPassword, null);
    }

    private static string GenerateTempPassword()
    {
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "!@#$";
        const string all = lower + upper + digits + special;

        var rng = new Random(Guid.NewGuid().GetHashCode());
        var chars = new char[12];
        chars[0] = upper[rng.Next(upper.Length)];
        chars[1] = lower[rng.Next(lower.Length)];
        chars[2] = digits[rng.Next(digits.Length)];
        chars[3] = special[rng.Next(special.Length)];
        for (var i = 4; i < chars.Length; i++)
            chars[i] = all[rng.Next(all.Length)];

        return new string(chars.OrderBy(_ => rng.Next()).ToArray());
    }
}
