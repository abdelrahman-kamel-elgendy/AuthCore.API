using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using AuthCore.API.Configs;
using AuthCore.API.Models;

namespace AuthCore.API.Data;

public class DbSeeder
{
    private readonly UserManager<UserModel> _userManager;
    private readonly RoleManager<IdentityRole<string>> _roleManager;
    private readonly SeedConfigs _seedConfigs;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        UserManager<UserModel> userManager,
        RoleManager<IdentityRole<string>> roleManager,
        IOptions<SeedConfigs> seedConfigs,
        ILogger<DbSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _seedConfigs = seedConfigs.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        // Seed Roles
        await SeedRoleAsync("Admin");
        await SeedRoleAsync("User");

        // Seed Admin User
        await SeedAdminUserAsync();

        _logger.LogInformation("Database seeding completed successfully");
    }

    private async Task SeedRoleAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var role = new IdentityRole<string>
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper()
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Role '{RoleName}' created successfully", roleName);
            }
            else
            {
                _logger.LogError("Failed to create role '{RoleName}': {Errors}",
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Role '{RoleName}' already exists", roleName);
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = _seedConfigs.Admin.Email;
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new UserModel
            {
                UserName = _seedConfigs.Admin.Username,
                Email = _seedConfigs.Admin.Email,
                FirstName = _seedConfigs.Admin.FirstName,
                LastName = _seedConfigs.Admin.LastName,
                EmailConfirmed = true, // Admin doesn't need email confirmation
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, _seedConfigs.Admin.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                await _userManager.AddToRoleAsync(adminUser, "User");
                _logger.LogInformation("Admin user '{AdminEmail}' created successfully", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure admin has Admin role
            if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Admin role added to existing user '{AdminEmail}'", adminEmail);
            }

            _logger.LogInformation("Admin user '{AdminEmail}' already exists", adminEmail);
        }
    }
}