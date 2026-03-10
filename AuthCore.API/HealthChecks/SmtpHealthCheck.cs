using System.Net.Sockets;
using AuthCore.API.Configs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AuthCore.API.HealthChecks;

public class SmtpHealthCheck(IOptions<SmtpConfigs> smtpOptions) : IHealthCheck
{
    private readonly SmtpConfigs _smtp = smtpOptions.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcpClient = new TcpClient();

            await tcpClient.ConnectAsync(_smtp.Host, _smtp.Port, cancellationToken);

            return tcpClient.Connected
                ? HealthCheckResult.Healthy($"SMTP reachable at {_smtp.Host}:{_smtp.Port}.")
                : HealthCheckResult.Unhealthy($"Could not connect to SMTP at {_smtp.Host}:{_smtp.Port}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"SMTP check failed for {_smtp.Host}:{_smtp.Port}.",
                exception: ex
            );
        }
    }
}