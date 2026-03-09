using AuthCore.API.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthCore.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(UserManager<UserModel> userManager, IConfiguration configuration)
    {
        var email = configuration["Seed:Admin:Email"] ?? "admin@authcore.com";
        var password = configuration["Seed:Admin:Password"] ?? "Admin@123456";
        var firstName = configuration["Seed:Admin:FirstName"] ?? "Super";
        var lastName = configuration["Seed:Admin:LastName"] ?? "Admin";
        var userName = configuration["Seed:Admin:UserName"] ?? "superadmin";

        if (await userManager.FindByEmailAsync(email) is not null) return;

        var admin = new UserModel
        {
            FirstName = firstName,
            LastName = lastName,
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            await userManager.AddToRoleAsync(admin, "User");
        }
    }
}