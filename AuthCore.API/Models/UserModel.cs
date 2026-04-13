using Microsoft.AspNetCore.Identity;

namespace AuthCore.API.Models;

public class UserModel : IdentityUser
{
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string LastName { get; set; }

    public string? ProfileURL { get; set; }
    public DateTime? BirthDate { get; set; }

    public bool IsBlocked { get; set; } = false;
    public DateTime? BlockedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }


    // Navigation properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public virtual ICollection<UserPhone> UserPhones { get; set; } = [];
    public virtual ICollection<UserAddress> UserAddresses { get; set; } = [];
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];

}