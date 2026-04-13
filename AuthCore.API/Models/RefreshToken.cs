namespace AuthCore.API.Models;

public class RefreshToken
{
    public Guid TokenId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByIp { get; set; } = string.Empty;

    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive { get; set; } = true;


    // Navigation properties
    public virtual UserModel User { get; set; } = null!;
    public virtual RefreshToken? ReplacedByToken { get; set; }
}