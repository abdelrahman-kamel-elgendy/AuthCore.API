using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class JwtConfigs
{
    public const string SectionName = "JWT";

    [Required(ErrorMessage = "JWT SecretKey is required.")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters.")]
    public string SecretKey { get; init; } = string.Empty;

    [Required(ErrorMessage = "JWT ValidIssuer is required.")]
    public string ValidIssuer { get; init; } = string.Empty;

    [Required(ErrorMessage = "JWT ValidAudience is required.")]
    public string ValidAudience { get; init; } = string.Empty;

    [Range(1, 720, ErrorMessage = "Access token expiry must be between 1 and 720 minutes.")]
    public int AccessTokenExpiryMinutes { get; init; } = 60;

    [Range(1, 30, ErrorMessage = "Refresh token expiry must be between 1 and 30 days.")]
    public int RefreshTokenExpiryDays { get; init; } = 7;
}