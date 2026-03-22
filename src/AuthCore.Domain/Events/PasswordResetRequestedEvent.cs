namespace AuthCore.Domain.Events;

public record PasswordResetRequestedEvent(
    Guid UserId,
    string Email,
    string ResetToken,
    DateTime OccurredAt
)
{
    public PasswordResetRequestedEvent(Guid userId, string email, string resetToken)
        : this(userId, email, resetToken, DateTime.UtcNow) { }
}