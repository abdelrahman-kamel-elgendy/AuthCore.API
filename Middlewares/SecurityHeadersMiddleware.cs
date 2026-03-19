namespace AuthCore.API.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.XXSSProtection = "1; mode=block";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            if (context.Request.IsHttps)
                headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";

            headers.ContentSecurityPolicy = context.Request.Path.StartsWithSegments("/swagger") && env.IsDevelopment()
                ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;"
                : "default-src 'none'; frame-ancestors 'none';";

            return Task.CompletedTask;
        });

        await next(context);
    }
}