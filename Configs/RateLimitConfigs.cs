using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.Configs;

public class RateLimitConfigs
{
    public const string SectionName = "RateLimit";

    public RateLimitPolicy Login { get; init; } = new(5, 1);
    public RateLimitPolicy Register { get; init; } = new(3, 5);
    public RateLimitPolicy ForgotPassword { get; init; } = new(3, 15);
    public RateLimitPolicy Global { get; init; } = new(60, 1);
}

public class RateLimitPolicy
{
    public RateLimitPolicy() { }

    public RateLimitPolicy(int permitLimit, int windowMinutes)
    {
        PermitLimit = permitLimit;
        WindowMinutes = windowMinutes;
    }

    [Range(1, 1000, ErrorMessage = "PermitLimit must be between 1 and 1000.")]
    public int PermitLimit { get; init; }

    [Range(1, 1440, ErrorMessage = "WindowMinutes must be between 1 and 1440.")]
    public int WindowMinutes { get; init; }
}