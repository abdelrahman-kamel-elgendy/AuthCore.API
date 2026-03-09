using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class SeedConfigs
{
    public const string SectionName = "Seed";

    [Required]
    public AdminSeedConfigs Admin { get; init; } = new();
}

public class AdminSeedConfigs
{
    [Required(ErrorMessage = "Seed Admin Email is required.")]
    [EmailAddress(ErrorMessage = "Seed Admin Email must be a valid email address.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin Password is required.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin FirstName is required.")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin LastName is required.")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin UserName is required.")]
    public string UserName { get; init; } = string.Empty;
}