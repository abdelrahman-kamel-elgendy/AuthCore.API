using System.Net;
using System.Text.Json;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;

namespace AuthCore.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IWebHostEnvironment _env = env;
    private readonly ILogger _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ApiResponse<object> apiResponse = new(false);

        switch (exception)
        {
            case Exceptions.ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Status = HttpStatusCode.BadRequest;
                apiResponse.Message = validationEx.Message;
                apiResponse.ValidationErrors = validationEx.Errors;
                _logger.LogWarning("Validation error on {Method} {Path} — {Errors}",
                    context.Request.Method,
                    context.Request.Path,
                    string.Join(", ", validationEx.Errors.SelectMany(e => e.Value)));
                break;

            case BadRequestException badRequestEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Status = HttpStatusCode.BadRequest;
                apiResponse.Message = badRequestEx.Message;
                _logger.LogWarning("Bad request on {Method} {Path} — {Message}",
                    context.Request.Method, context.Request.Path, badRequestEx.Message);
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Status = HttpStatusCode.NotFound;
                apiResponse.Message = notFoundEx.Message;
                _logger.LogWarning("Not found on {Method} {Path} — {Message}",
                    context.Request.Method, context.Request.Path, notFoundEx.Message);
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Status = HttpStatusCode.Unauthorized;
                apiResponse.Message = unauthorizedEx.Message;
                _logger.LogWarning("Unauthorized on {Method} {Path} — {Message}",
                    context.Request.Method, context.Request.Path, unauthorizedEx.Message);
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                apiResponse.Status = HttpStatusCode.Forbidden;
                apiResponse.Message = forbiddenEx.Message;
                _logger.LogWarning("Forbidden on {Method} {Path} — {Message}",
                    context.Request.Method, context.Request.Path, forbiddenEx.Message);
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                apiResponse.Status = HttpStatusCode.Conflict;
                apiResponse.Message = conflictEx.Message;
                _logger.LogWarning("Conflict on {Method} {Path} — {Message}",
                    context.Request.Method, context.Request.Path, conflictEx.Message);
                break;

            case TooManyRequestsException tooManyEx:
                response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                apiResponse.Status = HttpStatusCode.TooManyRequests;
                apiResponse.Message = tooManyEx.Message;
                if (tooManyEx.RetryAfterSeconds > 0)
                    response.Headers.RetryAfter = tooManyEx.RetryAfterSeconds.ToString();
                _logger.LogWarning("Rate limit exceeded on {Method} {Path} — retry after {RetryAfter}s",
                    context.Request.Method, context.Request.Path, tooManyEx.RetryAfterSeconds);
                break;

            case NotImplementedException notImplEx:
                response.StatusCode = (int)HttpStatusCode.NotImplemented;
                apiResponse.Status = HttpStatusCode.NotImplemented;
                apiResponse.Message = notImplEx.Message;
                _logger.LogWarning("Not implemented on {Method} {Path} — {Message}",
                    context.Request.Method, context.Request.Path, notImplEx.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Status = HttpStatusCode.InternalServerError;
                apiResponse.Message = "An internal server error occurred. Please try again later.";
                if (_env.IsDevelopment())
                    apiResponse.Errors = [exception.Message, exception.StackTrace ?? string.Empty];
                // LogError includes full exception + stack trace automatically
                _logger.LogError(exception,
                    "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                break;
        }

        await response.WriteAsync(JsonSerializer.Serialize(
            apiResponse,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}