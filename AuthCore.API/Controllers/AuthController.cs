using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;
using AuthCore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthCore.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto) =>
        StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<AuthResponseDto>(
                HttpStatusCode.Created,
                true,
                "Registration successful. Please check your email for confirmation.",
                await _authService.RegisterAsync(registerDto)
            ));

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token) =>
        Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Email confirmed successfully. You can now log in.",
            await _authService.ConfirmEmailAsync(new ConfirmEmailDto { UserId = userId, Token = token })
        ));

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto) =>
        Ok(new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Login successful.",
            await _authService.LoginAsync(loginDto)
        ));

    [HttpPost("refresh-token")]
    [EnableRateLimiting("global")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenDto) =>
        Ok(new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Token refreshed successfully.",
            await _authService.RefreshTokenAsync(refreshTokenDto)
        ));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout() =>
        Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Logged out successfully.",
            await _authService.LogoutAsync(CurrentUserId)
        ));

    [HttpPost("forgot-password")]
    [EnableRateLimiting("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto) =>
        Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Reset link has been sent.",
            await _authService.ForgotPasswordAsync(dto)
        ));

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Password reset successfully. Please log in with your new password.",
            null
        ));
    }


    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedException("User identity could not be resolved from token.");
}