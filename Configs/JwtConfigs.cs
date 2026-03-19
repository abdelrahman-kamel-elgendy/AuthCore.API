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

    public int AccessTokenExpiryMinutes { get; init; } = 60;    // default: 1 hour
    public int RefreshTokenExpiryDays { get; init; } = 7;     // default: 7 days
}