using System.ComponentModel.DataAnnotations;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Validators;

namespace AuthCore.API.DTOs.User;

public class UpdateProfileRequest
{
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters!")]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2, ErrorMessage = "Middle name must be between 2 and 50 characters!")]
    public string? MiddleName { get; set; }
    
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters!")]
    public string? LastName { get; set; }


    [Url(ErrorMessage = "Invalid URL format!")]
    [StringLength(200, ErrorMessage = "Profile URL cannot exceed 200 characters!")]
    public string? ProfileURL { get; set; }

    [DataType(DataType.Date)]
    [CustomValidation(typeof(DateValidator), nameof(DateValidator.Validate))]
    public DateTime? BirthDate { get; set; }
}