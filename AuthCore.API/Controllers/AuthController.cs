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
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IEmailService emailService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);

        await _emailService.SendEmailAsync(
            result.Email!,
            "Confirm your AuthCore account",
            EmailService.Render("../Templates/Email/ConfirmEmail.html", new() {
                    { "FirstName", result.FirstName ?? throw new BadRequestException("No name to send email!")},
                    { "ConfirmUrl", $"{_configuration["AppBaseUrl"] ?? "http://localhost:5000"}/api/auth/confirm-email?userId={result.UserId}&token={result.Token}"},
                    { "Year", DateTime.UtcNow.Year.ToString()}
            })
        );

        return Ok(new ApiResponse<AuthResponseDto>(
            HttpStatusCode.Created,
            true,
            "Registration successful. Please check your email for confirmation.",
            result
        ));
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var result = await _authService.ConfirmEmailAsync(new ConfirmEmailDto { UserId = userId, Token = token });

        string loginUrl = $"{_configuration["AppBaseUrl"] ?? "http://localhost:5000"}/login";
        await _emailService.SendEmailAsync(
            result.Email!,
            "Confirm your AuthCore account",
            EmailService.Render("../Templates/Email/WelcomeEmail.html", new() {
                { "FirstName", result.FirstName ?? throw new BadRequestException("No name to send email!") },
                { "UserName", result.UserName! },
                { "Email", result.Email! },
                { "LoginUrl", loginUrl},
                { "Year", DateTime.UtcNow.Year.ToString() }
            }));

        return Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Email confirmed successfully.You can now log in.",
            new { LoginUrl = loginUrl, UserEmail = result.Email, Name = result.FirstName }
        ));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        return Ok(new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Login successful.",
            await _authService.LoginAsync(loginDto)
        ));
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        return Ok(new ApiResponse<AuthResponseDto>(
            HttpStatusCode.OK,
            true,
            "Token refreshed successfully.",
            await _authService.RefreshTokenAsync(refreshTokenDto)
        ));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)) ?? throw new UnauthorizedException();

        await _authService.LogoutAsync(userId);

        return Ok(new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Logged out successfully.",
            null
        ));
    }
}
