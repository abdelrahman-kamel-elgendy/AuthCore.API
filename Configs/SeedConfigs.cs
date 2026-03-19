using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class SeedConfigs
{
    public const string SectionName = "Seed";

    [Required]
    public AdminSeedConfig Admin { get; init; } = new();
}

public class AdminSeedConfig
{
    [Required(ErrorMessage = "Seed Admin Email is required.")]
    [EmailAddress(ErrorMessage = "Seed Admin Email must be a valid email address.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin Password is required!")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Seed Admin Password must be at least 8 characters!")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
       ErrorMessage = "Seed Admin Password must have at least 1 uppercase, 1 lowercase, 1 number, and 1 special character.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin FirstName is required.")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin LastName is required.")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Seed Admin UserName is required.")]
    public string UserName { get; init; } = string.Empty;
}