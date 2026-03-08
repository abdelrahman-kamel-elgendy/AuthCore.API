namespace AuthCore.API.Exceptions;

public class NotFoundException : ApiException
{
    public NotFoundException(string resource, string key) : base($"Resource '{resource}' with identifier '{key}' was not found.", 404) { }
}
