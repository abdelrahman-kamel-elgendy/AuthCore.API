namespace AuthCore.API.DTOs;

public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfileURL { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
}