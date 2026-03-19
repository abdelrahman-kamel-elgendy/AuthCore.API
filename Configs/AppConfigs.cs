using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class AppConfigs
{
    public const string SectionName = "App";

    [Required(ErrorMessage = "AppBaseUrl is required.")]
    [Url(ErrorMessage = "AppBaseUrl must be a valid URL.")]
    public string BaseUrl { get; init; } = string.Empty;
}