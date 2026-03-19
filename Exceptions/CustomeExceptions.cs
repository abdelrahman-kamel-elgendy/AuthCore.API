using System.Net;

namespace AuthCore.API.Exceptions;

public class ForbiddenException(string message = "You don't have permission to access this resource.") : ApiException(message, HttpStatusCode.Forbidden) { }
public class UnauthorizedException(string message = "You are not authorized to access this resource.") : ApiException(message, HttpStatusCode.Unauthorized) { }
public class ConflictException(string message, string? details = null) : ApiException(message, HttpStatusCode.Conflict, details) { }
public class NotFoundException(string resource, string key) : ApiException($"Resource '{resource}' with identifier '{key}' was not found.", HttpStatusCode.NotFound) { }
public class BadRequestException(string message, string? details = null) : ApiException(message, HttpStatusCode.BadRequest, details) { }

public class TooManyRequestsException(string message = "Too many requests. Please slow down and try again later.", int retryAfterSeconds = 0) : ApiException(message, HttpStatusCode.TooManyRequests)
{
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
public class ValidationException(IDictionary<string, string[]> errors) : ApiException("One or more validation errors occurred.", HttpStatusCode.BadRequest)
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}