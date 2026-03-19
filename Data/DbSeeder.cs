using AuthCore.API.Configs;
using AuthCore.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthCore.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        UserManager<UserModel> userManager,
        ApplicationDbContext db,
        SeedConfigs configs,
        ILogger logger)
    {
        var admin = configs.Admin;

        logger.LogInformation("DbSeeder: Data base seeding...");
        logger.LogInformation("DbSeeder: checking if admin {Email} exists...", admin.Email);
        var adminExists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == admin.Email);

        if (adminExists)
        {
            logger.LogInformation("DbSeeder: admin {Email} already exists - skipping seed", admin.Email);
            return;
        }

        logger.LogInformation("DbSeeder: creating admin user {Email}...", admin.Email);

        var adminUser = new UserModel
        {
            UserName = admin.UserName,
            Email = admin.Email,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, admin.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Admin seeding failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");


        await userManager.AddToRoleAsync(adminUser, "Admin");

        logger.LogInformation("DbSeeder: admin {Email} seeded successfully with UserId {UserId}", admin.Email, adminUser.Id);
    }
}