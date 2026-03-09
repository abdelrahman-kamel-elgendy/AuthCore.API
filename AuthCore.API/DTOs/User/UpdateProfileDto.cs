using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.DTOs.User;

public class UpdateProfileDto
{
    [StringLength(50, MinimumLength = 2)]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    public string? LastName { get; set; }

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [Url, StringLength(200)]
    public string? ProfileURL { get; set; }

    public DateTime? BirthDate { get; set; }
}