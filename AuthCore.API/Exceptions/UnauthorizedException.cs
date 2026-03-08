namespace AuthCore.API.Exceptions;

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "You are not authorized to access this resource.") : base(message, 401)
    {
    }
}