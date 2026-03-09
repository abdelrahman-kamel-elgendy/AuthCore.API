using AuthCore.API.DTOs;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.DTOs.User;

namespace AuthCore.API.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetProfileAsync(string userId);
    Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto dto);
}