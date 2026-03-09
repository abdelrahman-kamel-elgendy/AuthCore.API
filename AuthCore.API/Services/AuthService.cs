using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AuthCore.API.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await _authRepository.UserExistsByEmailAsync(dto.Email))
            throw new ConflictException("Email is already registered.");

        if (await _authRepository.UserExistsByUserNameAsync(dto.Username))
            throw new ConflictException("Username is already taken.");

        var user = new UserModel
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            UserName = dto.Username,
            Email = dto.Email,
            EmailConfirmed = false,
            ProfileURL = dto.ProfileURL,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            BirthDate = dto.BirthDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _authRepository.CreateUserAsync(user, dto.Password);
        await _authRepository.AddToRoleAsync(user, "User");

        _logger.LogInformation("New user registered: {Email}", user.Email);

        return new AuthResponseDto
        {
            Token = Uri.EscapeDataString(await _authRepository.GenerateEmailConfirmationTokenAsync(user)),
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName
        };
    }

    public async Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto dto)
    {
        var user = await _authRepository.GetUserByIdAsync(dto.UserId)
            ?? throw new NotFoundException("User", dto.UserId);

        if (user.EmailConfirmed)
            throw new BadRequestException("Email is already confirmed.");

        var result = await _authRepository.ConfirmEmailAsync(user, dto.Token);
        if (!result.Succeeded)
            throw new BadRequestException("Invalid or expired confirmation token.");

        _logger.LogInformation("Email confirmed for: {Email}", user.Email);

        return new AuthResponseDto
        {
            Email = user.Email,
            UserName = user.UserName,
            FirstName = user.FirstName
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _authRepository.GetUserByEmailAsync(dto.Email);

        // Generic error — never reveal whether the email or password was wrong
        if (user == null || !await _authRepository.CheckPasswordAsync(user, dto.Password))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.EmailConfirmed)
            throw new UnauthorizedException("Please confirm your email address before logging in.");

        if (!user.IsActive)
            throw new ForbiddenException("Your account has been deactivated.");

        var roles = await _authRepository.GetUserRolesAsync(user);
        var accessToken = GenerateAccessToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        await _authRepository.SaveRefreshTokenAsync(user, refreshToken);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(accessToken),
            RefreshToken = refreshToken,
            Expiration = accessToken.ValidTo,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = [.. roles]
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
    {
        var user = await _authRepository.GetUserByRefreshTokenAsync(dto.RefreshToken);

        if (user == null)
            throw new UnauthorizedException("Invalid refresh token.");

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired. Please log in again.");

        var roles = await _authRepository.GetUserRolesAsync(user);
        var newAccessToken = GenerateAccessToken(user, roles);
        var newRefreshToken = GenerateRefreshToken();

        // Rotate the refresh token
        await _authRepository.SaveRefreshTokenAsync(user, newRefreshToken);

        _logger.LogInformation("Token refreshed for: {Email}", user.Email);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
            RefreshToken = newRefreshToken,
            Expiration = newAccessToken.ValidTo,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = [.. roles]
        };
    }

    public async Task LogoutAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new UnauthorizedException();

        await _authRepository.RevokeRefreshTokenAsync(user);
        _logger.LogInformation("User logged out: {UserId}", userId);
    }   


    private JwtSecurityToken GenerateAccessToken(UserModel user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JWT");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("is_active", user.IsActive.ToString())
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new JwtSecurityToken(
            issuer: jwtSettings["ValidIssuer"],
            audience: jwtSettings["ValidAudience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}