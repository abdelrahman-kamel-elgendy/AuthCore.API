using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class RedisConfigs
{
    public const string SectionName = "Redis";

    [Required(ErrorMessage = "Redis ConnectionString is required.")]
    public string ConnectionString { get; init; } = string.Empty;
}