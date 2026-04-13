using Microsoft.EntityFrameworkCore;
using AuthCore.API.Data;
using AuthCore.API.Models;
using AuthCore.API.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace AuthCore.API.Repositories;

public class AuthRepository(ApplicationDbContext context) : IAuthRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserPhones)
            .Include(u => u.UserAddresses)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> GetUserByIdAsync(string userId)
    {
        return await _context.Users
            .Include(u => u.UserPhones)
            .Include(u => u.UserAddresses)
            .FirstOrDefaultAsync(u => u.Id.Equals(userId));
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task CreateRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string revokedByIp)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(string userId, string revokedByIp)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
        }

        _context.RefreshTokens.UpdateRange(tokens);
        await _context.SaveChangesAsync();
    }

    public async Task CreatePasswordResetTokenAsync(PasswordResetToken resetToken)
    {
        await _context.PasswordResetTokens.AddAsync(resetToken);
        await _context.SaveChangesAsync();
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
    {
        return await _context.PasswordResetTokens
            .Include(prt => prt.User)
            .FirstOrDefaultAsync(prt => prt.Token == token && !prt.IsUsed && prt.ExpiryDate > DateTime.UtcNow);
    }

    public async Task MarkPasswordResetTokenAsUsedAsync(PasswordResetToken resetToken)
    {
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        _context.PasswordResetTokens.Update(resetToken);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsEmailConfirmedAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.EmailConfirmed ?? false;
    }

    public async Task UpdateUserAsync(UserModel user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public Task<UserModel?> GetUserByUserNameAsync(string userName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CheckPasswordAsync(UserModel user, string password)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> CreateUserAsync(UserModel user, string password)
    {
        throw new NotImplementedException();
    }

    Task<IdentityResult> IAuthRepository.UpdateUserAsync(UserModel user)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> DeleteUserAsync(UserModel user)
    {
        throw new NotImplementedException();
    }

    public Task<IList<string>> GetUserRolesAsync(UserModel user)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> AddToRoleAsync(UserModel user, string role)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> RemoveFromRoleAsync(UserModel user, string role)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsInRoleAsync(UserModel user, string role)
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateEmailConfirmationTokenAsync(UserModel user)
    {
        throw new NotImplementedException();
    }

    public Task<string> GeneratePasswordResetTokenAsync(UserModel user)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> ConfirmEmailAsync(UserModel user, string token)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> ResetPasswordAsync(UserModel user, string token, string newPassword)
    {
        throw new NotImplementedException();
    }

    public Task<UserModel?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }

    public Task SaveRefreshTokenAsync(UserModel user, string refreshToken, int expiryDays = 7)
    {
        throw new NotImplementedException();
    }

    public Task RevokeRefreshTokenAsync(UserModel user)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UserExistsByEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UserExistsByUserNameAsync(string userName)
    {
        throw new NotImplementedException();
    }
}