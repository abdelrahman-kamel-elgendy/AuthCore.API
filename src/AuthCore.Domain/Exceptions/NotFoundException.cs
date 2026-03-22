namespace AuthCore.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.", Common.ErrorCodes.UserNotFound) { }

    public NotFoundException(string message)
        : base(message, Common.ErrorCodes.UserNotFound) { }
}