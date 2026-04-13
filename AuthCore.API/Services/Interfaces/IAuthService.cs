using AuthCore.API.DTOs.Auth;

namespace AuthCore.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string ipAddress);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task LogoutAsync(string refreshToken, string accessToken);
    Task ConfirmEmailAsync(string userId, string token);
    Task ForgotPasswordAsync(string email, string requestUrl);
    Task ResetPasswordAsync(ResetPasswordRequestDto request);
}