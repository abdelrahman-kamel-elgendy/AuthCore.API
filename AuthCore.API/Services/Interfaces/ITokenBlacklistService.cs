namespace AuthCore.API.Services.Interfaces;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string token, TimeSpan? expiryDuration = null);
    Task<bool> IsTokenBlacklistedAsync(string token);
    Task RemoveExpiredTokensAsync();
}