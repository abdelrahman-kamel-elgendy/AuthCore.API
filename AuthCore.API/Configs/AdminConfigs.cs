using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class AdminConfigs
{
    [Required(ErrorMessage = "Admin email is required.")]
    [EmailAddress(ErrorMessage = "Admin email must be a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin password is required.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin first name is required.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin last name is required.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin username is required.")]
    public string Username { get; set; } = string.Empty;
}