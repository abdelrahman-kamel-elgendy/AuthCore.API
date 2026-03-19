using Microsoft.AspNetCore.Identity;

namespace AuthCore.API.Models;

public class UserModel : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? ProfileURL { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}