namespace AuthCore.API.Models;

public class UserPhone
{
    public string PhoneId { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneConfirmed { get; set; } = false;

    public bool IsPrimary { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;


    // Navigation properties
    public virtual UserModel User { get; set; } = null!;
}