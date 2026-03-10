using System.Net;
using AuthCore.API.DTOs;
using AuthCore.API.Models;
using AuthCore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService;

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var result = await _adminService.GetAllUsersAsync(pageNumber, pageSize);
        var metadata = PaginationMetadata.From(result);
        Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(metadata));

        return Ok(
            new ApiResponse<PagedList<UserResponseDto>>(
                HttpStatusCode.OK,
                true,
                "Users retrieved successfully.",
                result
            ));
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(string userId) => Ok(
        new ApiResponse<UserResponseDto>(
            HttpStatusCode.OK,
            true,
            $"User {userId} retrieved successfully.",
            await _adminService.GetUserByIdAsync(userId)
        ));

    [HttpPost("users/{userId}/promote")]
    public async Task<IActionResult> PromoteUser(string userId) => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            $"User {userId} promoted to Admin.",
            await _adminService.PromoteToAdminAsync(userId)
        ));


    [HttpPost("users/{userId}/demote")]
    public async Task<IActionResult> DemoteUser(string userId) => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            $"User {userId} demoted from Admin.",
            await _adminService.DemoteFromAdminAsync(userId)
        ));

    [HttpPost("users/{userId}/deactivate")]
    public async Task<IActionResult> DeactivateUser(string userId) => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            $"User {userId} has been deactivated.",
            await _adminService.DeactivateUserAsync(userId)
        ));

    [HttpPost("users/{userId}/activate")]
    public async Task<IActionResult> ActivateUser(string userId) => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            $"User {userId} has been activated.",
            await _adminService.ActivateUserAsync(userId)
        ));


    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId) => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            $"User {userId} has been permanently deleted.",
            await _adminService.DeleteUserAsync(userId)
            ));
}