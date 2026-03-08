namespace AuthCore.API.Exceptions;

public abstract class ApiException : Exception
{
    public int StatusCode { get; }
    public string? Details { get; }

    protected ApiException(string message, int statusCode = 500, string? details = null) : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }
}