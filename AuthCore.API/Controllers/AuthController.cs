using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Models;
using AuthCore.API.Services.Interfaces;

namespace AuthCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///         "firstName": "John",
    ///         "lastName": "Doe",
    ///         "username": "johndoe",
    ///         "email": "john@example.com",
    ///         "password": "Secret@123",
    ///         "confirmPassword": "Secret@123"
    ///     }
    /// </remarks>
    /// <response code="201">Registration successful</response>
    /// <response code="400">Invalid registration data</response>
    /// <response code="409">User already exists</response>
    /// <response code="429">Too many requests</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 429)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RegisterAsync(request, ipAddress);
        return StatusCode(201, new ApiResponse<AuthResponseDto>(true, result, "Registration successful. Please check your email for confirmation."));
    }

    /// <summary>
    /// Login to existing account
    /// </summary>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="429">Too many requests</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 429)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.LoginAsync(request, ipAddress);
        return Ok(new ApiResponse<AuthResponseDto>(true, result, "Login successful."));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
        return Ok(new ApiResponse<AuthResponseDto>(true, result, "Token refreshed successfully."));
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? request)
    {
        var refreshToken = request?.RefreshToken ?? string.Empty;
        var accessToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last() ?? string.Empty;
        await _authService.LogoutAsync(refreshToken, accessToken);
        return Ok(new ApiResponse<object>(true, null, "Logged out successfully."));
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        await _authService.ConfirmEmailAsync(userId, token);
        return Ok(new ApiResponse<object>(true, null, "Email confirmed successfully."));
    }

    /// <summary>
    /// Request password reset link
    /// </summary>
    /// <remarks>Always returns 200 even if email doesn't exist (security)</remarks>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 429)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var requestUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        await _authService.ForgotPasswordAsync(request.Email, requestUrl);
        return Ok(new ApiResponse<object>(true, null, "If your email is registered, you will receive a password reset link."));
    }

    /// <summary>
    /// Reset password using token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new ApiResponse<object>(true, null, "Password reset successfully. Please login with your new password."));
    }

    private string GetIpAddress()
    {
        // Check for forwarded IP behind proxy
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString().Split(',').First().Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}