using AuthCore.API.DTOs;
using AuthCore.API.Models;

namespace AuthCore.API.Services.Interfaces;

public interface IAdminService
{
    // Queries
    Task<PagedList<UserResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize);
    Task<PagedList<UserResponseDto>> GetDeletedUsersAsync(int pageNumber, int pageSize);
    Task<UserResponseDto> GetUserByIdAsync(string userId);

    // Role management
    Task<UserResponseDto> PromoteToAdminAsync(string userId);
    Task<UserResponseDto> DemoteFromAdminAsync(string userId);

    // Account state
    Task<UserResponseDto> ActivateUserAsync(string userId);
    Task<UserResponseDto> DeactivateUserAsync(string userId);
    Task<UserResponseDto> ForceLogoutAsync(string userId);

    // Soft delete
    Task<UserResponseDto> DeleteUserAsync(string userId);
    Task<UserResponseDto> RestoreUserAsync(string userId);
}