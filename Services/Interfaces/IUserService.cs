using AuthCore.API.DTOs;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.DTOs.User;

namespace AuthCore.API.Services.Interfaces;

public interface IUserService
{
    Task<ProfileResponseDto> GetProfileAsync(string userId);
    Task<ProfileResponseDto> UpdateProfileAsync(string userId, UpdateProfileRequest dto);
    Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordRequestDto dto);
}