namespace AuthCore.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var start = DateTime.UtcNow;

        await _next(context);

        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var status = context.Response.StatusCode;
        var clientIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var host = context.Request.Host.Value;
        var scheme = context.Request.Scheme;

        // Silence Swagger traffic — log at Debug so it's invisible at default level
        var isSwagger = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

        if (isSwagger)
            _logger.LogDebug(
                "HTTP {Method} {Scheme}://{Host}{Path} responded {StatusCode} in {Elapsed:0.00}ms | IP: {ClientIP}",
                method, scheme, host, path, status, elapsed, clientIp);

        else
            _logger.LogInformation(
                "HTTP {Method} {Scheme}://{Host}{Path} responded {StatusCode} in {Elapsed:0.00}ms | IP: {ClientIP} | UA: {UserAgent}",
                method, scheme, host, path, status, elapsed, clientIp, userAgent);
    }
}