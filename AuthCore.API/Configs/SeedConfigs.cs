using System.ComponentModel.DataAnnotations;
using AuthCore.API.DTOs.Auth;

namespace AuthCore.API.Configs;

public class SeedConfigs
{
    public const string SectionName = "Seed";

    [Required]
    public RegisterDto Admin { get; init; } = new();
}