using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.DTOs.Auth;

public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}
