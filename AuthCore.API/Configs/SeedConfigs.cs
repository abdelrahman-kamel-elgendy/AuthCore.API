using System.ComponentModel.DataAnnotations;
using AuthCore.API.DTOs.Auth;

namespace AuthCore.API.Configs;

public class SeedConfigs
{
    public const string SectionName = "Seed";

    [Required(ErrorMessage = "Admin configuration is required.")]
    public AdminConfigs Admin { get; set; } = new();
}