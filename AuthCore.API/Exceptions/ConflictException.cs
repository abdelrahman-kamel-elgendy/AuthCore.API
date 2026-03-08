namespace AuthCore.API.Exceptions;

public class ConflictException : ApiException
{
    public ConflictException(string message, string? details = null) 
        : base(message, 409, details)
    {
    }
}
