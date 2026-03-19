using AuthCore.API.DTOs;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services.Interfaces;

namespace AuthCore.API.Services;

public class AdminService(
    IAuthRepository authRepository,
    ITokenBlacklistService tokenBlacklist) : IAdminService
{
    private readonly IAuthRepository _authRepository = authRepository;
    private readonly ITokenBlacklistService _tokenBlacklist = tokenBlacklist;

    // == Queries ===============================================================

    public async Task<PagedList<UserResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize)
    {
        var (users, total) = await _authRepository.GetAllUsersPagedAsync(pageNumber, pageSize);
        var dtos = await MapToDtosAsync(users);
        return new PagedList<UserResponseDto>(dtos, total, pageNumber, pageSize);
    }

    public async Task<PagedList<UserResponseDto>> GetDeletedUsersAsync(int pageNumber, int pageSize)
    {
        var (users, total) = await _authRepository.GetDeletedUsersPagedAsync(pageNumber, pageSize);
        var dtos = await MapToDtosAsync(users);
        return new PagedList<UserResponseDto>(dtos, total, pageNumber, pageSize);
    }

    public async Task<UserResponseDto> GetUserByIdAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        return MapToDto(user, await _authRepository.GetUserRolesAsync(user));
    }

    // == Role Management =======================================================

    public async Task<UserResponseDto> PromoteToAdminAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (await _authRepository.IsInRoleAsync(user, "Admin"))
            throw new BadRequestException("User is already an Admin.");

        await _authRepository.AddToRoleAsync(user, "Admin");

        return MapToDto(user, await _authRepository.GetUserRolesAsync(user));
    }

    public async Task<UserResponseDto> DemoteFromAdminAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (!await _authRepository.IsInRoleAsync(user, "Admin"))
            throw new BadRequestException("User is not an Admin.");

        await _authRepository.RemoveFromRoleAsync(user, "Admin");

        return MapToDto(user, await _authRepository.GetUserRolesAsync(user));
    }

    // == Account State =========================================================

    public async Task<UserResponseDto> ActivateUserAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (user.IsActive)
            throw new BadRequestException("User is already active.");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);

        return MapToDto(user, await _authRepository.GetUserRolesAsync(user));
    }

    public async Task<UserResponseDto> DeactivateUserAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (!user.IsActive)
            throw new BadRequestException("User is already deactivated.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);
        await _authRepository.RevokeRefreshTokenAsync(user);

        return MapToDto(user, await _authRepository.GetUserRolesAsync(user));
    }

    public async Task<UserResponseDto> ForceLogoutAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        // Revoke refresh token — blocks silent token renewal immediately
        await _authRepository.RevokeRefreshTokenAsync(user);

        return MapToDto(user, await _authRepository.GetUserRolesAsync(user));
    }

    // == Soft Delete ===========================================================

    public async Task<UserResponseDto> DeleteUserAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var roles = await _authRepository.GetUserRolesAsync(user);
        await _authRepository.SoftDeleteUserAsync(user);

        return MapToDto(user, roles);
    }

    public async Task<UserResponseDto> RestoreUserAsync(string userId)
    {
        var user = await _authRepository.GetDeletedUserByIdAsync(userId)
            ?? throw new NotFoundException("Deleted user", userId);

        await _authRepository.RestoreUserAsync(user);

        return MapToDto(user, await _authRepository.GetUserRolesAsync(user));
    }

    // == Helpers ===============================================================

    private async Task<List<UserResponseDto>> MapToDtosAsync(List<UserModel> users)
    {
        var dtos = new List<UserResponseDto>();
        foreach (var user in users)
            dtos.Add(MapToDto(user, await _authRepository.GetUserRolesAsync(user)));
        return dtos;
    }

    private static UserResponseDto MapToDto(UserModel user, IList<string> roles) => new()
    {
        Id = user.Id,
        UserName = user.UserName!,
        Email = user.Email!,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        ProfileURL = user.ProfileURL,
        Address = user.Address,
        BirthDate = user.BirthDate,
        EmailConfirmed = user.EmailConfirmed,
        IsActive = user.IsActive,
        IsDeleted = user.IsDeleted,
        DeletedAt = user.DeletedAt,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Roles = [.. roles]
    };
}