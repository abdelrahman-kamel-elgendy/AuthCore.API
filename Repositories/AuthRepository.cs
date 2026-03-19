using AuthCore.API.Data;
using AuthCore.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthCore.API.Repositories;

public class AuthRepository(
    UserManager<UserModel> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db) : IAuthRepository
{
    private readonly UserManager<UserModel> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly ApplicationDbContext _db = db;

    // == User Management =======================================================

    public async Task<UserModel?> GetUserByIdAsync(string userId)
        => await _userManager.FindByIdAsync(userId);

    public async Task<UserModel?> GetUserByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email);

    public async Task<UserModel?> GetUserByUserNameAsync(string userName)
        => await _userManager.FindByNameAsync(userName);

    public async Task<bool> CheckPasswordAsync(UserModel user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task<IdentityResult> CreateUserAsync(UserModel user, string password)
        => await _userManager.CreateAsync(user, password);

    public async Task<IdentityResult> UpdateUserAsync(UserModel user)
        => await _userManager.UpdateAsync(user);

    public async Task<IdentityResult> DeleteUserAsync(UserModel user)
        => await _userManager.DeleteAsync(user);

    // == Soft Delete ===========================================================

    public async Task SoftDeleteUserAsync(UserModel user)
    {
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = DateTime.MinValue;

        await _userManager.UpdateAsync(user);
    }

    public async Task RestoreUserAsync(UserModel user)
    {
        user.IsDeleted = false;
        user.DeletedAt = null;
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
    }

    public async Task<UserModel?> GetDeletedUserByIdAsync(string userId)
        => await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted);

    // == Paginated Queries =====================================================

    /// <summary>
    /// Returns active (non-deleted) users paged. Global query filter handles
    /// the IsDeleted exclusion automatically — no manual WHERE needed.
    /// </summary>
    public async Task<(List<UserModel> Users, int Total)> GetAllUsersPagedAsync(
        int pageNumber, int pageSize)
    {
        var query = _db.Users.OrderBy(u => u.CreatedAt);
        var total = await query.CountAsync();
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (users, total);
    }

    /// <summary>
    /// Returns only soft-deleted users paged, ordered by most recently deleted.
    /// Uses IgnoreQueryFilters to bypass the global IsDeleted filter.
    /// </summary>
    public async Task<(List<UserModel> Users, int Total)> GetDeletedUsersPagedAsync(
        int pageNumber, int pageSize)
    {
        var query = _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.IsDeleted)
            .OrderByDescending(u => u.DeletedAt);

        var total = await query.CountAsync();
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (users, total);
    }

    // == Role Management =======================================================

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

    // == Token Management ======================================================

    public async Task<string> GenerateEmailConfirmationTokenAsync(UserModel user)
        => await _userManager.GenerateEmailConfirmationTokenAsync(user);

    public async Task<string> GeneratePasswordResetTokenAsync(UserModel user)
        => await _userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<IdentityResult> ConfirmEmailAsync(UserModel user, string token)
        => await _userManager.ConfirmEmailAsync(user, token);

    public async Task<IdentityResult> ResetPasswordAsync(UserModel user, string token, string newPassword)
        => await _userManager.ResetPasswordAsync(user, token, newPassword);

    // == Refresh Token Management ==============================================

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

    // == Existence Checks ======================================================

    public async Task<bool> UserExistsByEmailAsync(string email)
        => await _userManager.Users.AnyAsync(u => u.Email == email);

    public async Task<bool> UserExistsByUserNameAsync(string userName)
        => await _userManager.Users.AnyAsync(u => u.UserName == userName);
}