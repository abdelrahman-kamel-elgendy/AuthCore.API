using AuthCore.API.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthCore.API.Repositories;

public interface IAuthRepository
{
    // User management
    Task<UserModel?> GetUserByIdAsync(string userId);
    Task<UserModel?> GetUserByEmailAsync(string email);
    Task<UserModel?> GetUserByUserNameAsync(string userName);
    Task<bool> CheckPasswordAsync(UserModel user, string password);
    Task<IdentityResult> CreateUserAsync(UserModel user, string password);
    Task<IdentityResult> UpdateUserAsync(UserModel user);
    Task<IdentityResult> DeleteUserAsync(UserModel user);        // hard delete — kept for internal use
    Task SoftDeleteUserAsync(UserModel user);                    // sets IsDeleted + DeletedAt
    Task RestoreUserAsync(UserModel user);                       // clears IsDeleted + DeletedAt
    Task<UserModel?> GetDeletedUserByIdAsync(string userId);     // bypasses global query filter

    // Paginated queries (used by AdminService — keeps DbContext out of the service layer)
    Task<(List<UserModel> Users, int Total)> GetAllUsersPagedAsync(int pageNumber, int pageSize);
    Task<(List<UserModel> Users, int Total)> GetDeletedUsersPagedAsync(int pageNumber, int pageSize);

    // Role management
    Task<IList<string>> GetUserRolesAsync(UserModel user);
    Task<IdentityResult> AddToRoleAsync(UserModel user, string role);
    Task<IdentityResult> RemoveFromRoleAsync(UserModel user, string role);
    Task<bool> IsInRoleAsync(UserModel user, string role);

    // Token management
    Task<string> GenerateEmailConfirmationTokenAsync(UserModel user);
    Task<string> GeneratePasswordResetTokenAsync(UserModel user);
    Task<IdentityResult> ConfirmEmailAsync(UserModel user, string token);
    Task<IdentityResult> ResetPasswordAsync(UserModel user, string token, string newPassword);

    // Refresh token management
    Task<UserModel?> GetUserByRefreshTokenAsync(string refreshToken);
    Task SaveRefreshTokenAsync(UserModel user, string refreshToken, int expiryDays = 7);
    Task RevokeRefreshTokenAsync(UserModel user);

    // Existence checks
    Task<bool> UserExistsByEmailAsync(string email);
    Task<bool> UserExistsByUserNameAsync(string userName);
}