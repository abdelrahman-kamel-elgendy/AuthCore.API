using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.DTOs.Auth;

public class ConfirmEmailRequestDto
{
    [Required(ErrorMessage = "User ID is required.")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}