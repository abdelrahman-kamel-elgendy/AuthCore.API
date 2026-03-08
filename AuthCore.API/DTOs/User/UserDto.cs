namespace AuthCore.API.DTOs.User;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public List<string>? Roles { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
