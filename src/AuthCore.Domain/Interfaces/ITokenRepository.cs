using AuthCore.Domain.Entities;

namespace AuthCore.Domain.Interfaces;

public interface ITokenRepository
{
    // Refresh tokens
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default);
    Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensAsync(Guid userId, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct = default);

    // Password reset tokens
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token, CancellationToken ct = default);
    Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken ct = default);
    Task InvalidatePreviousResetTokensAsync(Guid userId, CancellationToken ct = default);

    // Cleanup
    Task DeleteExpiredTokensAsync(CancellationToken ct = default);
}