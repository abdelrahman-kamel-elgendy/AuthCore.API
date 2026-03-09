namespace AuthCore.API.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.XXSSProtection = "1; mode=block";
            headers.Referer = "strict-origin-when-cross-origin";
            headers.ContentSecurityPolicy = "default-src 'none'; " + "frame-ancestors 'none';";
            if (context.Request.IsHttps)
                headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";

            return Task.CompletedTask;
        });

        await next(context);
    }
}