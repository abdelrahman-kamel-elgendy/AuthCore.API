namespace AuthCore.API.Exceptions;

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "You don't have permission to access this resource.") : base(message, 403)
    {
    }
}