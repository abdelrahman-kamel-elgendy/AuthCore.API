using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class SmtpConfigs
{
    public const string SectionName = "Smtp";

    [Required(ErrorMessage = "Smtp Host is required.")]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp Port must be between 1 and 65535.")]
    public int Port { get; init; } = 587;

    [Required(ErrorMessage = "Smtp Username is required.")]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Smtp Password is required.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Smtp FromName is required.")]
    public string FromName { get; init; } = string.Empty;

    public bool EnableSsl { get; init; } = true;
}