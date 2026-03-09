using AuthCore.API.DTOs.Auth;

namespace AuthCore.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto);
    Task<AuthResponseDto> LogoutAsync(string id);
    Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto dto);
    Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}
