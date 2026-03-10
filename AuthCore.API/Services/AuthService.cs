using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AuthCore.API.Services;

public class AuthService(IAuthRepository authRepository, IEmailService emailService,
    IConfiguration configuration) : IAuthService
{
    private readonly IAuthRepository _authRepository = authRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
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
            BirthDate = dto.BirthDate
        };

        var result = await _authRepository.CreateUserAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        result = await _authRepository.AddToRoleAsync(user, "User");
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        var encodedToken = Uri.EscapeDataString(await _authRepository.GenerateEmailConfirmationTokenAsync(user));
        var confirmUrl = $"{_configuration["AppBaseUrl"] ?? "http://localhost:5000"}/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        var body = EmailService.Render("../Templates/Email/ConfirmEmail.html", new Dictionary<string, string>
        {
            { "FirstName", user.FirstName },
            { "ConfirmUrl", confirmUrl },
            { "Year", DateTime.UtcNow.Year.ToString() }
        });

        await _emailService.SendEmailAsync(user.Email!, "Confirm your AuthCore account", body);

        return new AuthResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Token = encodedToken
        };
    }

    public async Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailRequestDto dto)
    {
        var user = await _authRepository.GetUserByIdAsync(dto.UserId) ?? throw new NotFoundException("User", dto.UserId);

        if (user.EmailConfirmed)
            throw new BadRequestException("Email is already confirmed.");

        var result = await _authRepository.ConfirmEmailAsync(user, dto.Token);
        if (!result.Succeeded)
            throw new BadRequestException("Invalid or expired confirmation token.");

        var baseUrl = _configuration["AppBaseUrl"] ?? "http://localhost:5000";
        var roles = await _authRepository.GetUserRolesAsync(user);

        var body = EmailService.Render("../Templates/Email/WelcomeEmail.html", new Dictionary<string, string>
        {
            { "FirstName", user.FirstName },
            { "UserName", user.UserName! },
            { "Email", user.Email! },
            { "Role", roles.FirstOrDefault() ?? "User" },
            { "LoginUrl", $"{baseUrl}/api/auth/login" },
            { "Year", DateTime.UtcNow.Year.ToString() }
        });

        await _emailService.SendEmailAsync(user.Email!, "Welcome to AuthCore 🎉", body);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email
        };
    }

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
    {
        var user = await _authRepository.GetUserByEmailAsync(dto.Email) ?? throw new NotFoundException("user", dto.Email);

        if (!user.EmailConfirmed)
            throw new BadRequestException("Email not confirmed!");

        var encodedToken = Uri.EscapeDataString(await _authRepository.GeneratePasswordResetTokenAsync(user));
        var resetUrl = $"{_configuration["AppBaseUrl"] ?? "http://localhost:5000"}/api/auth/reset-password?userId={user.Id}&token={encodedToken}";

        var body = EmailService.Render("../Templates/Email/ResetPassword.html", new Dictionary<string, string>
        {
            { "FirstName", user.FirstName },
            { "ResetUrl", resetUrl },
            { "Year", DateTime.UtcNow.Year.ToString() }
        });

        await _emailService.SendEmailAsync(user.Email!, "Reset your AuthCore password", body);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Token = encodedToken
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        var user = await _authRepository.GetUserByIdAsync(dto.UserId) ?? throw new NotFoundException("User", dto.UserId);
        var result = await _authRepository.ResetPasswordAsync(user, Uri.UnescapeDataString(dto.Token), dto.Password);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        await _authRepository.RevokeRefreshTokenAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _authRepository.GetUserByEmailAsync(dto.Email) ?? throw new NotFoundException("user", dto.Email);

        if (!user.IsActive)
            throw new ForbiddenException("Your account has been deactivated.");

        if (!await _authRepository.CheckPasswordAsync(user, dto.Password))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.EmailConfirmed)
            throw new UnauthorizedException("Please confirm your email address before logging in.");


        var roles = await _authRepository.GetUserRolesAsync(user);
        var accessToken = GenerateAccessToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        await _authRepository.SaveRefreshTokenAsync(user, refreshToken);

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

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var user = await _authRepository.GetUserByRefreshTokenAsync(dto.RefreshToken) ?? throw new UnauthorizedException("Invalid refresh token.");

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired. Please log in again.");

        var roles = await _authRepository.GetUserRolesAsync(user);
        var newAccessToken = GenerateAccessToken(user, roles);
        var newRefreshToken = GenerateRefreshToken();

        await _authRepository.SaveRefreshTokenAsync(user, newRefreshToken);

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

    public async Task<AuthResponseDto> LogoutAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new UnauthorizedException("User not exists!");

        await _authRepository.RevokeRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName
        };
    }

    private JwtSecurityToken GenerateAccessToken(UserModel user, IList<string> roles)
    {
        var jwtConfigs = _configuration.GetSection("JWT");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigs["SecretKey"]!));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name,  user.UserName!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("is_active", user.IsActive.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new JwtSecurityToken(
            issuer: jwtConfigs["ValidIssuer"],
            audience: jwtConfigs["ValidAudience"],
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