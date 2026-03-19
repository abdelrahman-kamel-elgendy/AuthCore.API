using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Asp.Versioning;
using AuthCore.API.DTOs;
using AuthCore.API.DTOs.Auth;
using AuthCore.API.DTOs.User;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthCore.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
     ?? throw new UnauthorizedAccessException();

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile() => Ok(
        new ApiResponse<ProfileResponseDto>(
            HttpStatusCode.OK,
            true,
            "Profile retrieved successfully.",
            await _userService.GetProfileAsync(GetCurrentUserId())
        ));

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest dto) => Ok(
        new ApiResponse<ProfileResponseDto>(
            HttpStatusCode.OK,
            true,
            "Profile updated successfully.",
            await _userService.UpdateProfileAsync(GetCurrentUserId(), dto)
        ));

    [HttpPut("me/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto) => Ok(
        new ApiResponse<object>(
            HttpStatusCode.OK,
            true,
            "Password changed successfully. Please log in again.",
            await _userService.ChangePasswordAsync(GetCurrentUserId(), dto)
        ));
}