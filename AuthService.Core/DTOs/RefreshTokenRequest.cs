using System.ComponentModel.DataAnnotations;

namespace AuthService.Core.DTOs;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; }

    [Required(ErrorMessage = "Access token is required.")]
    public string AccessToken { get; set; }
}