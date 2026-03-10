using AuthCore.API.Services.Interfaces;
using StackExchange.Redis;

namespace AuthCore.API.Services;

public class TokenBlacklistService(IConnectionMultiplexer redis, ILogger<TokenBlacklistService> logger) : ITokenBlacklistService
{
    // Key prefix keeps blacklist entries namespaced and easy to identify in Redis
    private const string KeyPrefix = "blacklist:jti:";
    private readonly IDatabase _redis = redis.GetDatabase();
    private readonly ILogger<TokenBlacklistService> _logger = logger;

    public async Task RevokeAsync(string jti, DateTime expiry)
    {
        var ttl = expiry - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            _logger.LogWarning("Attempted to revoke already-expired token {Jti}", jti);
            return;
        }

        var key = KeyPrefix + jti;
        await _redis.StringSetAsync(key, "1", ttl);
        _logger.LogInformation("Token {Jti} blacklisted. Expires in {TTL}", jti, ttl);
    }

    public async Task<bool> IsRevokedAsync(string jti) => await _redis.KeyExistsAsync(KeyPrefix + jti);
}