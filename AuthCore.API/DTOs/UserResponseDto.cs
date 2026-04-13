
namespace AuthCore.API.DTOs.User;

public class UserResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? ProfileURL { get; set; }
    public DateTime? BirthDate { get; set; }

    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<string> Roles { get; set; } = [];
    public List<UserPhoneDto> Phones { get; set; } = [];
    public List<UserAddressDto> Addresses { get; set; } = [];
}