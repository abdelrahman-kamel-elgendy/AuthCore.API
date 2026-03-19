using System.Net;

namespace AuthCore.API.Exceptions;

public abstract class ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string? details = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? Details { get; } = details;
}