namespace AuthCore.Domain.Common;

public static class ErrorCodes
{
    // Auth
    public const string InvalidCredentials = "AUTH_001";
    public const string EmailAlreadyExists = "AUTH_002";
    public const string EmailNotVerified = "AUTH_003";
    public const string AccountBanned = "AUTH_004";
    public const string InvalidRefreshToken = "AUTH_005";
    public const string RefreshTokenExpired = "AUTH_006";
    public const string InvalidResetToken = "AUTH_007";
    public const string ResetTokenExpired = "AUTH_008";

    // User
    public const string UserNotFound = "USR_001";
    public const string RoleNotFound = "USR_002";
    public const string RoleAlreadyAssigned = "USR_003";
    public const string InvalidCurrentPassword = "USR_004";

    // General
    public const string ValidationFailed = "GEN_001";
    public const string Unauthorized = "GEN_002";
    public const string Forbidden = "GEN_003";
    public const string InternalError = "GEN_004";
}