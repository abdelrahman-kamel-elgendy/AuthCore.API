using AuthCore.API.DTOs.User;
using AuthCore.API.Models;

namespace AuthCore.API.Services.Interfaces;

public interface IAdminService
{
    Task<PagedList<UserResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize);
    Task<UserResponseDto> GetUserAsync(string userId);
    Task PromoteToAdminAsync(string userId);
    Task DemoteFromAdminAsync(string userId);
    Task ActivateUserAsync(string userId);
    Task DeactivateUserAsync(string userId, string revokedByIp);
    Task DeleteUserAsync(string userId);
}