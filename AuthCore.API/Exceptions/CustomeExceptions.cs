using System.Net;
using AuthCore.API.Exceptions;

namespace AuthCore.API.Exceptions;

public class ForbiddenException(string message = "You don't have permission to access this resource.") : ApiException(message, HttpStatusCode.Forbidden) { }
public class UnauthorizedException(string message = "You are not authorized to access this resource.") : ApiException(message, HttpStatusCode.Unauthorized) { }
public class ConflictException(string message, string? details = null) : ApiException(message, HttpStatusCode.Conflict, details) { }
public class NotFoundException(string resource, string key) : ApiException($"Resource '{resource}' with identifier '{key}' was not found.", HttpStatusCode.NotFound) { }
public class BadRequestException(string message, string? details = null) : ApiException(message, HttpStatusCode.BadRequest, details) { }
public class NotImplementedException(string message = "functionality is planned but not yet implemented.", string? details = null) : ApiException(message, HttpStatusCode.NotImplemented, details) { }
public class ValidationException(IDictionary<string, string[]> errors) : ApiException("One or more validation errors occurred.", HttpStatusCode.BadRequest)
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}