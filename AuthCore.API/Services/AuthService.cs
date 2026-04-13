using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using AuthCore.API.Configs;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services.Interfaces;

namespace AuthCore.API.Services;

public class AuthService(
    UserManager<UserModel> userManager,
    IAuthRepository authRepository,
    IEmailService emailService,
    ILogger<AuthService> logger,
    IOptions<JwtConfigs> jwtConfigs,
    IOptions<AppConfigs> appConfigs) : IAuthService
{
    private readonly UserManager<UserModel> _userManager = userManager;
    private readonly IAuthRepository _authRepository = authRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly JwtConfigs _jwtConfigs = jwtConfigs.Value;
    private readonly AppConfigs _appConfigs = appConfigs.Value;

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string ipAddress)
    {
        _logger.LogInformation("Registration attempt for email: {Email} from IP: {IpAddress}", request.Email, ipAddress);

        // Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed - Email already exists: {Email}", request.Email);
            throw new ConflictException("User with this email already exists");
        }

        existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed - Username already exists: {Username}", request.Username);
            throw new ConflictException("User with this username already exists");
        }

        // Create user
        var user = new UserModel
        {
            UserName = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            ProfileURL = request.ProfileURL,
            BirthDate = request.BirthDate,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Registration failed for {Email}: {Errors}", request.Email, errors);
            throw new BadRequestException(errors);
        }

        // Add default role
        await _userManager.AddToRoleAsync(user, "User");

        // Generate email confirmation token
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(emailToken);
        var confirmationUrl = $"{_appConfigs.BaseUrl}/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        // Send confirmation email
        await _emailService.SendConfirmationEmailAsync(user.Email, user.FirstName, confirmationUrl);

        _logger.LogInformation("User registered successfully: {Email} with ID: {UserId}", user.Email, user.Id);

        // Generate tokens
        var authResponse = await GenerateAuthResponseAsync(user, ipAddress);

        return authResponse;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress)
    {
        _logger.LogInformation("Login attempt for email: {Email} from IP: {IpAddress}", request.Email, ipAddress);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed - User not found: {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        // Check if user is active
        if (user.IsBlocked || user.IsDeleted)
        {
            _logger.LogWarning("Login failed - Account is deactivated: {Email}", request.Email);
            throw new UnauthorizedException("Account is deactivated. Please contact support.");
        }

        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed - Email not confirmed: {Email}", request.Email);
            throw new UnauthorizedException("Please confirm your email before logging in.");
        }

        // Check password
        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            _logger.LogWarning("Login failed - Invalid password for user: {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        // Generate tokens
        var authResponse = await GenerateAuthResponseAsync(user, ipAddress, request.RememberMe);

        return authResponse;
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        _logger.LogInformation("Refresh token attempt from IP: {IpAddress}", ipAddress);

        var storedToken = await _authRepository.GetRefreshTokenAsync(refreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            _logger.LogWarning("Invalid or inactive refresh token used from IP: {IpAddress}", ipAddress);
            throw new UnauthorizedException("Invalid refresh token");
        }

        // Check if token has been revoked (token reuse detection)
        if (storedToken.RevokedAt != null)
        {
            _logger.LogWarning("Attempted reuse of revoked refresh token for user: {UserId}", storedToken.UserId);
            // Revoke all user tokens as security measure
            await _authRepository.RevokeAllUserTokensAsync(storedToken.UserId, ipAddress);
            throw new UnauthorizedException("Token reuse detected. All tokens have been revoked.");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null || user.IsBlocked || user.IsDeleted)
        {
            _logger.LogWarning("User not found or inactive for refresh token: {UserId}", storedToken.UserId);
            throw new UnauthorizedException("Invalid refresh token");
        }

        // Revoke old token
        await _authRepository.RevokeRefreshTokenAsync(storedToken, ipAddress);

        // Generate new tokens
        var authResponse = await GenerateAuthResponseAsync(user, ipAddress);

        _logger.LogInformation("Tokens refreshed successfully for user: {Email}", user.Email);

        return authResponse;
    }

    public async Task LogoutAsync(string refreshToken, string accessToken)
    {
        _logger.LogInformation("Logout attempt");

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var storedToken = await _authRepository.GetRefreshTokenAsync(refreshToken);
            if (storedToken != null && storedToken.IsActive)
            {
                await _authRepository.RevokeRefreshTokenAsync(storedToken, "logout");
                _logger.LogInformation("Refresh token revoked for user: {UserId}", storedToken.UserId);
            }
        }

        // Note: Access token will expire naturally (1 hour)
        // For blacklisting, you would need a token blacklist cache
    }

    public async Task ConfirmEmailAsync(string userId, string token)
    {
        _logger.LogInformation("Email confirmation attempt for user ID: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Email confirmation failed - User not found: {UserId}", userId);
            throw new NotFoundException("User", userId);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Email confirmation failed for user: {Email}", user.Email);
            throw new BadRequestException("Invalid or expired confirmation token");
        }

        // Send welcome email
        var roles = await _userManager.GetRolesAsync(user);
        var loginUrl = $"{_appConfigs.FrontendUrl}/login";
        await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, user.UserName!, roles.FirstOrDefault() ?? "User", loginUrl);

        _logger.LogInformation("Email confirmed successfully for user: {Email}", user.Email);
    }

    public async Task ForgotPasswordAsync(string email, string requestUrl)
    {
        // Always return 200 for security (don't reveal if email exists)
        _logger.LogInformation("Password reset requested for email: {Email}", email);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.EmailConfirmed)
        {
            _logger.LogWarning("Password reset requested for non-existent or unconfirmed email: {Email}", email);
            return; // Silent fail for security
        }

        // Generate password reset token
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(resetToken);

        // Store token in our custom table
        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = resetToken,
            ExpiryDate = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        await _authRepository.CreatePasswordResetTokenAsync(passwordResetToken);

        var resetUrl = $"{_appConfigs.FrontendUrl}/reset-password?userId={user.Id}&token={encodedToken}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetUrl);

        _logger.LogInformation("Password reset email sent to: {Email}", email);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        _logger.LogInformation("Password reset attempt for user ID: {UserId}", request.UserId);

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("Password reset failed - User not found: {UserId}", request.UserId);
            throw new BadRequestException("Invalid password reset request");
        }

        // Validate custom token
        var resetToken = await _authRepository.GetPasswordResetTokenAsync(request.Token);
        if (resetToken == null || resetToken.UserId != user.Id || resetToken.IsUsed || resetToken.ExpiryDate < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset failed - Invalid token for user: {Email}", user.Email);
            throw new BadRequestException("Invalid or expired reset token");
        }

        // Reset password
        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Password reset failed for user {Email}: {Errors}", user.Email, errors);
            throw new BadRequestException(errors);
        }

        // Mark token as used
        await _authRepository.MarkPasswordResetTokenAsUsedAsync(resetToken);

        // Revoke all refresh tokens (force re-login)
        await _authRepository.RevokeAllUserTokensAsync(user.Id, "password_reset");

        _logger.LogInformation("Password reset successfully for user: {Email}", user.Email);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(UserModel user, string ipAddress, bool rememberMe = false)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = GenerateAccessToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenExpiryDays = rememberMe ? 30 : _jwtConfigs.RefreshTokenExpiryDays;
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        await _authRepository.CreateRefreshTokenAsync(refreshTokenEntity);

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(_jwtConfigs.AccessTokenExpiryMinutes),
            UserId = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            Roles = [.. roles]
        };
    }

    private string GenerateAccessToken(UserModel user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfigs.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.UserName!),
            new(GetJti(), Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _jwtConfigs.ValidIssuer,
            audience: _jwtConfigs.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfigs.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetJti()
    {
        return JwtRegisteredClaimNames.Jti;
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}