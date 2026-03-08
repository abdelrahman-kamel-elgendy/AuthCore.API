namespace AuthCore.API.Exceptions;

public class BadRequestException : ApiException
{
    public BadRequestException(string message, string? details = null) 
        : base(message, 400, details)
    {
    }
}