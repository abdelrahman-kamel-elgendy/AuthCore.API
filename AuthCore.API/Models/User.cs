
using Microsoft.AspNetCore.Identity;

namespace AuthCore.API.Models;

public class User : IdentityUser {
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? ProfileURL { get; set; }
    public string? PhoneNumbeer { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}