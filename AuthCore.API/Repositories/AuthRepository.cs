using AuthCore.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthCore.API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly UserManager<UserModel> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthRepository(
        UserManager<UserModel> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AuthRepository> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // User Management
    public async Task<UserModel?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<UserModel?> GetUserByUserNameAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName);
    }

    public async Task<bool> CheckPasswordAsync(UserModel user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityResult> CreateUserAsync(UserModel user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> UpdateUserAsync(UserModel user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> DeleteUserAsync(UserModel user)
    {
        return await _userManager.DeleteAsync(user);
    }

    // Role Management
    public async Task<IList<string>> GetUserRolesAsync(UserModel user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IdentityResult> AddToRoleAsync(UserModel user, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(UserModel user, string role)
    {
        return await _userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<bool> IsInRoleAsync(UserModel user, string role)
    {
        return await _userManager.IsInRoleAsync(user, role);
    }

    // Token Management
    public async Task<string> GenerateEmailConfirmationTokenAsync(UserModel user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(UserModel user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<IdentityResult> ConfirmEmailAsync(UserModel user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<IdentityResult> ResetPasswordAsync(UserModel user, string token, string newPassword)
    {
        return await _userManager.ResetPasswordAsync(user, token, newPassword);
    }

    // User Existence Checks
    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        return await _userManager.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> UserExistsByUserNameAsync(string userName)
    {
        return await _userManager.Users.AnyAsync(u => u.UserName == userName);
    }
}