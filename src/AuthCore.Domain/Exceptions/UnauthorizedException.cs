namespace AuthCore.Domain.Exceptions;

public class UnauthorizedException(string message) : DomainException(message, Common.ErrorCodes.Unauthorized) { }