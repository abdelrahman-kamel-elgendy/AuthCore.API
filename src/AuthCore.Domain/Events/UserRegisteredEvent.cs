namespace AuthCore.Domain.Events;

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FirstName,
    DateTime OccurredAt
)
{
    public UserRegisteredEvent(Guid userId, string email, string firstName)
        : this(userId, email, firstName, DateTime.UtcNow) { }
}