namespace AuthCore.API.Services.Interfaces;

public interface ITokenBlacklistService
{
    Task RevokeAsync(string jti, DateTime expiry);
   Task<bool> IsRevokedAsync(string jti);
}