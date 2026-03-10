using AuthCore.API.DTOs;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.DTOs.User;
using AuthCore.API.Exceptions;
using AuthCore.API.Repositories;
using AuthCore.API.Services.Interfaces;

namespace AuthCore.API.Services;

public class UserService(IAuthRepository authRepository) : IUserService
{
    private readonly IAuthRepository _authRepository = authRepository;

    public async Task<ProfileDto> GetProfileAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("user", userId);
        return new ProfileDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            ProfileURL = user.ProfileURL,
            Address = user.Address,
            BirthDate = user.BirthDate
        };
    }

    public async Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("user", userId);

        if (dto.FirstName is not null)
            user.FirstName = dto.FirstName;
        if (dto.LastName is not null)
            user.LastName = dto.LastName;
        if (dto.PhoneNumber is not null)
            user.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null)
            user.Address = dto.Address;
        if (dto.ProfileURL is not null)
            user.ProfileURL = dto.ProfileURL;
        if (dto.BirthDate is not null)
            user.BirthDate = dto.BirthDate;

        user.UpdatedAt = DateTime.UtcNow;

        var result = await _authRepository.UpdateUserAsync(user);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        return await GetProfileAsync(userId);
    }

    public async Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("User", userId);

        if (!await _authRepository.CheckPasswordAsync(user, dto.CurrentPassword))
            throw new BadRequestException("Current password is incorrect.");

        var resetToken = await _authRepository.GeneratePasswordResetTokenAsync(user);
        var result = await _authRepository.ResetPasswordAsync(user, resetToken, dto.Password);

        if (!result.Succeeded)
            throw new ValidationException(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        await _authRepository.RevokeRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            UserName = user.UserName
        };
    }
}