using System.Net;
using System.Text.Json;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;

namespace AuthCore.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = new ApiResponse<object>
        {
            Success = false
        };

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = validationEx.Message;
                apiResponse.ValidationErrors = validationEx.Errors;
                break;

            case BadRequestException badRequestEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = badRequestEx.Message;
                apiResponse.Errors = new List<string> { badRequestEx.Details ?? badRequestEx.Message };
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = notFoundEx.Message;
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = unauthorizedEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                apiResponse.Message = forbiddenEx.Message;
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                apiResponse.Message = conflictEx.Message;
                apiResponse.Errors = new List<string> { conflictEx.Details ?? conflictEx.Message };
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = "An internal server error occurred. Please try again later.";

                if (_env.IsDevelopment())
                    apiResponse.Errors = new List<string> {
                        exception.Message,
                        exception.StackTrace ?? string.Empty
                    };

                break;
        }

        var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}