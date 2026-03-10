using AuthCore.API.DTOs;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.Exceptions;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthCore.API.Services;

public class AdminService(IAuthRepository authRepository,
    UserManager<UserModel> userManager) : IAdminService
{
    private readonly IAuthRepository _authRepository = authRepository;
    private readonly UserManager<UserModel> _userManager = userManager;

    public async Task<PagedList<UserResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize)
    {
        var query = _userManager.Users.OrderBy(u => u.CreatedAt);
        var total = await query.CountAsync();
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<UserResponseDto>();
        foreach (var user in users)
            dtos.Add(MapToDto(user, await _authRepository.GetUserRolesAsync(user)));

        return new PagedList<UserResponseDto>(dtos, total, pageNumber, pageSize);
    }

    public async Task<UserResponseDto> GetUserByIdAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("user", userId);
        var roles = await _authRepository.GetUserRolesAsync(user);
        return MapToDto(user, roles);
    }

    public async Task<AuthResponseDto> PromoteToAdminAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("User", userId);

        if (await _authRepository.IsInRoleAsync(user, "Admin"))
            throw new BadRequestException("User is already an Admin.");

        await _authRepository.AddToRoleAsync(user, "Admin");

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Roles = [.. await _authRepository.GetUserRolesAsync(user)]
        };
    }

    public async Task<AuthResponseDto> DemoteFromAdminAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("User", userId);

        if (!await _authRepository.IsInRoleAsync(user, "Admin"))
            throw new BadRequestException("User is not an Admin.");

        await _authRepository.RemoveFromRoleAsync(user, "Admin");

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Roles = [.. await _authRepository.GetUserRolesAsync(user)]
        };
    }

    public async Task<UserModel> DeactivateUserAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("User", userId);

        if (!user.IsActive)
            throw new BadRequestException("User is already deactivated.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);
        await _authRepository.RevokeRefreshTokenAsync(user);

        return user;
    }

    public async Task<UserModel> ActivateUserAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("User", userId);

        if (user.IsActive)
            throw new BadRequestException("User is already active.");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);

        return user;
    }

    public async Task<UserModel> DeleteUserAsync(string userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("User", userId);

        await _authRepository.DeleteUserAsync(user);

        return user;
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
        CreatedAt = user.CreatedAt,
        Roles = [.. roles]
    };
}