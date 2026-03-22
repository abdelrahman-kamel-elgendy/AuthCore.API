using AuthCore.Domain.Common;
using AuthCore.Domain.Enums;
using AuthCore.Domain.Events;
using AuthCore.Domain.Exceptions;

namespace AuthCore.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public string? PhoneNumber { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public bool IsEmailVerified { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; private set; } = new List<PasswordResetToken>();

    // ── Computed ──────────────────────────────────────────────────────────────
    public string FullName => $"{FirstName} {LastName}";

    // ── Factory ───────────────────────────────────────────────────────────────
    public static User Create(
        string email, string passwordHash,
        string firstName, string lastName,
        string? phoneNumber = null)
    {
        var user = new User
        {
            Email = email.ToLower().Trim(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PhoneNumber = phoneNumber
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.FirstName));
        return user;
    }

    // ── Behavior ──────────────────────────────────────────────────────────────
    public void UpdateProfile(string firstName, string lastName, string? phoneNumber = null)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePasswordHash(string newHash)
    {
        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin() =>
        LastLoginAt = DateTime.UtcNow;

    public void Ban()
    {
        if (Status == UserStatus.Banned)
            throw new DomainException("User is already banned.", ErrorCodes.AccountBanned);

        Status = UserStatus.Banned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unban()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        Status = UserStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasRole(string roleName) =>
        UserRoles.Any(ur => ur.Role?.Name == roleName);

    private User() { }
}