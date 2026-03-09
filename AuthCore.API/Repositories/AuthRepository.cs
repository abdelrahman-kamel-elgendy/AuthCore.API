using AuthCore.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthCore.API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly UserManager<UserModel> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(
        UserManager<UserModel> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AuthRepository> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // ── User Management ───────────────────────────────────────────────────────

    public async Task<UserModel?> GetUserByIdAsync(string userId)
        => await _userManager.FindByIdAsync(userId);

    public async Task<UserModel?> GetUserByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email);

    public async Task<UserModel?> GetUserByUserNameAsync(string userName)
        => await _userManager.FindByNameAsync(userName);

    public async Task<bool> CheckPasswordAsync(UserModel user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task<IdentityResult> CreateUserAsync(UserModel user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            _logger.LogWarning("Failed to create user {Email}: {Errors}", user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
       
        return result;
    }

    public async Task<IdentityResult> UpdateUserAsync(UserModel user) => await _userManager.UpdateAsync(user);

    public async Task<IdentityResult> DeleteUserAsync(UserModel user)
        => await _userManager.DeleteAsync(user);

    // ── Role Management ───────────────────────────────────────────────────────

    public async Task<IList<string>> GetUserRolesAsync(UserModel user)
        => await _userManager.GetRolesAsync(user);

    public async Task<IdentityResult> AddToRoleAsync(UserModel user, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(UserModel user, string role)
        => await _userManager.RemoveFromRoleAsync(user, role);

    public async Task<bool> IsInRoleAsync(UserModel user, string role)
        => await _userManager.IsInRoleAsync(user, role);

    public async Task<string> GenerateEmailConfirmationTokenAsync(UserModel user)
        => await _userManager.GenerateEmailConfirmationTokenAsync(user);

    public async Task<string> GeneratePasswordResetTokenAsync(UserModel user)
        => await _userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<IdentityResult> ConfirmEmailAsync(UserModel user, string token)
        => await _userManager.ConfirmEmailAsync(user, token);

    public async Task<IdentityResult> ResetPasswordAsync(UserModel user, string token, string newPassword)
        => await _userManager.ResetPasswordAsync(user, token, newPassword);

    public async Task<UserModel?> GetUserByRefreshTokenAsync(string refreshToken)
        => await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    public async Task SaveRefreshTokenAsync(UserModel user, string refreshToken, int expiryDays = 7)
    {
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(expiryDays);
        await _userManager.UpdateAsync(user);
    }

    public async Task RevokeRefreshTokenAsync(UserModel user)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = DateTime.MinValue;
        await _userManager.UpdateAsync(user);
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
        => await _userManager.Users.AnyAsync(u => u.Email == email);

    public async Task<bool> UserExistsByUserNameAsync(string userName)
        => await _userManager.Users.AnyAsync(u => u.UserName == userName);
}
