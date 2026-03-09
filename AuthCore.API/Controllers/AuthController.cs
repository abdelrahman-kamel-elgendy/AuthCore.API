using System.Security.Claims;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Models;
using AuthCore.API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthCore.API.Services;
using AuthCore.API.Exceptions;
using System.Net;

namespace AuthCore.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, IEmailService emailService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto) => Ok(
        new ApiResponse<AuthResponseDto>(
            HttpStatusCode.Created,
            true,
            "Registration successful. Please check your email for confirmation.",
            await _authService.RegisterAsync(registerDto)
        ));

    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token) => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Email confirmed successfully.You can now log in.",
            await _authService.ConfirmEmailAsync(new ConfirmEmailDto { UserId = userId, Token = token })
        ));

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto) => Ok(
        new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Login successful.",
            await _authService.LoginAsync(loginDto)
        ));

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto) => Ok(
        new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Token refreshed successfully.",
            await _authService.RefreshTokenAsync(refreshTokenDto)
        ));

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout() => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Logged out successfully.",
            await _authService.LogoutAsync((User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)) ?? throw new UnauthorizedException())
        ));
}
