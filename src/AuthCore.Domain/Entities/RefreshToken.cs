using AuthCore.Domain.Common;

namespace AuthCore.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedBy { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(
        Guid userId, string token, DateTime expiresAt,
        string? deviceInfo = null, string? ipAddress = null) =>
        new()
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedBy = replacedByToken;
        UpdatedAt = DateTime.UtcNow;
    }

    private RefreshToken() { }
}