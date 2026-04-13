using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Core.Entities;

public class RefreshToken
{
    [Key]
    public Guid TokenId { get; set; }

    public string Token { get; set; }
    public DateTime TokenExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => (RevokedAt != null && RevokedAt > DateTime.UtcNow) || TokenExpiryDate > DateTime.UtcNow;


    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; }
}