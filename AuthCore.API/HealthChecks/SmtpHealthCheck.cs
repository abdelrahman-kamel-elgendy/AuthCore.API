using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AuthCore.API.Configs;
using Microsoft.Extensions.Options;

namespace AuthCore.API.HealthChecks;

public class SmtpHealthCheck(IOptions<SmtpConfigs> smtpConfigs, ILogger<SmtpHealthCheck> logger) : IHealthCheck
{
    private readonly SmtpConfigs _smtpConfigs = smtpConfigs.Value;
    private readonly ILogger<SmtpHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(_smtpConfigs.Host, _smtpConfigs.Port);
            var timeoutTask = Task.Delay(5000, cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("SMTP health check timeout for {Host}:{Port}", _smtpConfigs.Host, _smtpConfigs.Port);
                return HealthCheckResult.Degraded($"SMTP connection timeout to {_smtpConfigs.Host}:{_smtpConfigs.Port}");
            }

            await connectTask;

            if (tcpClient.Connected)
            {
                tcpClient.Close();
                return HealthCheckResult.Healthy($"SMTP reachable at {_smtpConfigs.Host}:{_smtpConfigs.Port}");
            }

            return HealthCheckResult.Unhealthy($"SMTP not reachable at {_smtpConfigs.Host}:{_smtpConfigs.Port}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP health check failed");
            return HealthCheckResult.Unhealthy($"SMTP check failed: {ex.Message}");
        }
    }
}