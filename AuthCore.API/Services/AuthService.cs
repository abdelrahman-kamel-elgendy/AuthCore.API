using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthCore.API.DTOs;
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

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        if (await _authRepository.UserExistsByEmailAsync(registerDto.Email))
            throw new ConflictException("Email is already registered");

        if (await _authRepository.UserExistsByUserNameAsync(registerDto.Username))
            throw new ConflictException("Username is already taken");

        // Create new user
        UserModel user = new UserModel
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,

            UserName = registerDto.Username,
            Email = registerDto.Email,
            EmailConfirmed = false,

            ProfileURL = registerDto.ProfileURL != null ? registerDto.ProfileURL : null,

            PhoneNumber = registerDto.PhoneNumber != null ? registerDto.PhoneNumber : null,

            Address = registerDto.Address != null ? registerDto.Address : null,

            BirthDate = registerDto.BirthDate != null ? registerDto.BirthDate : null,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,

            IsActive = true
        };

        await _authRepository.CreateUserAsync(user, registerDto.Password);
        await _authRepository.AddToRoleAsync(user, "User");

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Registration successful. Please check your email for confirmation."
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        // Get user by email
        var user = await _authRepository.GetUserByEmailAsync(loginDto.Email);

        // Check if user exists
        if (user == null)
            throw new NotFoundException("user", loginDto.Email);

        // Check if user is active
        if (!user.IsActive)
            throw new ForbiddenException("Account is deactivated");

        // Check password
        if (!await _authRepository.CheckPasswordAsync(user, loginDto.Password))
            throw new UnauthorizedException("Invalid email or password");

        // Get user roles
        var roles = await _authRepository.GetUserRolesAsync(user);

        // Generate token
        var token = await GenerateJwtTokenAsync(user, roles);

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Login successful",
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = token.ValidTo,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles.ToList()
        };
    }

    private async Task<JwtSecurityToken> GenerateJwtTokenAsync(UserModel user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JWT");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("user_id", user.Id),
            new Claim("is_active", user.IsActive.ToString())
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Create token
        return new JwtSecurityToken(
            issuer: jwtSettings["ValidIssuer"],
            audience: jwtSettings["ValidAudience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );
    }
}