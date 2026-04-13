namespace AuthCore.API.Models;

public class PasswordResetToken
{
    public string TokenId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;


    // Navigation properties
    public virtual UserModel User { get; set; } = null!;
}