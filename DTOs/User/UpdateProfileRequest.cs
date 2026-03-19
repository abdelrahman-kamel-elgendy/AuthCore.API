using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.DTOs.User;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "First name is required!")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters!")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required!")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters!")]
    public string LastName { get; set; } = string.Empty;

    [Url(ErrorMessage = "Invalid URL format!")]
    [StringLength(200, ErrorMessage = "Profile URL cannot exceed 200 characters!")]
    public string? ProfileURL { get; set; }

    [Phone(ErrorMessage = "Invalid phone number!")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters!")]
    public string? PhoneNumber { get; set; }

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters!")]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    [CustomValidation(typeof(DateValidator), nameof(DateValidator.Validate))]
    public DateTime? BirthDate { get; set; }
}