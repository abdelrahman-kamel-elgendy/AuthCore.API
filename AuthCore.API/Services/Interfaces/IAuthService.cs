using AuthCore.API.DTOs.Auth;

namespace AuthCore.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task LogoutAsync(string userId);
    Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto);
}
