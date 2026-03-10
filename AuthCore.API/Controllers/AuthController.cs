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

    // == Helpers ===============================================================

    /// <summary>
    /// Resolves the current user's ID from JWT claims.
    /// Checks both ClaimTypes.NameIdentifier (ASP.NET Identity default) and
    /// the standard JWT "sub" claim — whichever is present.
    /// </summary>
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedException("User identity could not be resolved from token.");

    // == Endpoints =============================================================

    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto) =>
        StatusCode(
            StatusCodes.Status201Created,
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
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId,
        [FromQuery] string token) =>
        Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Email confirmed successfully. You can now log in.",
            await _authService.ConfirmEmailAsync(new ConfirmEmailDto { UserId = userId, Token = token })
        ));

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto) =>
        Ok(new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Login successful.",
            await _authService.LoginAsync(loginDto)
        ));

    [HttpPost("refresh-token")]
    [EnableRateLimiting("global")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto) =>
        Ok(new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Token refreshed successfully.",
            await _authService.RefreshTokenAsync(refreshTokenDto)
        ));

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout() =>
        Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Logged out successfully.",
            await _authService.LogoutAsync(CurrentUserId)
        ));

    [HttpPost("forgot-password")]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto) =>
        Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Reset link has been sent.",
            await _authService.ForgotPasswordAsync(dto)
        ));

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Password reset successfully. Please log in with your new password.",
            null
        ));
    }
}