using AuthCore.API.DTOs;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Models;

namespace AuthCore.API.Services.Interfaces;

public interface IAdminService
{
    Task<PagedList<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize);
    Task<UserDto> GetUserByIdAsync(string userId);
    Task<AuthResponseDto> PromoteToAdminAsync(string userId);
    Task<AuthResponseDto> DemoteFromAdminAsync(string userId);
    Task<UserModel> DeactivateUserAsync(string userId);
    Task<UserModel> ActivateUserAsync(string userId);
    Task<UserModel> DeleteUserAsync(string userId);
}