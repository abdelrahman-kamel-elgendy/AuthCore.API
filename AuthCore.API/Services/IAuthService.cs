using AuthCore.API.DTOs;
using AuthCore.API.DTOs.Auth;

namespace AuthCore.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
}