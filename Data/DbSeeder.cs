using AuthCore.API.Models;
using AuthCore.API.Configs;
using Microsoft.AspNetCore.Identity;

namespace AuthCore.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(UserManager<UserModel> userManager, SeedConfigs configs)
    {
        var admin = configs.Admin;

        var existingAdmin = await userManager.FindByEmailAsync(admin.Email);
        if (existingAdmin is not null)
            return;

        var adminUser = new UserModel
        {
            UserName = admin.Username,
            Email = admin.Email,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, admin.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Admin seeding failed: {errors}");
        }

        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}