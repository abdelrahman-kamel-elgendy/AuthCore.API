using AuthCore.API.DTOs.Auth;

namespace AuthCore.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<AuthResponseDto> LogoutAsync(string id, string accessToken);  // ← added accessToken
    Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailRequestDto dto);
    Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto);
    Task ResetPasswordAsync(ResetPasswordRequestDto dto);
}
