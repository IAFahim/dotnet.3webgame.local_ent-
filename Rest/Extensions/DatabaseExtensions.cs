using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rest.Data;
using Rest.Models;

namespace Rest.Extensions;

public static class DatabaseExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            await context.Database.MigrateAsync();

            var seedEnabled = configuration.GetValue("SeedData:Enabled", true);

            if (!seedEnabled)
            {
                logger.LogInformation("Database seeding is disabled in configuration");
                return;
            }

            if (!await userManager.Users.AnyAsync())
            {
                await SeedTestUsersAsync(userManager, logger);
            }
            else
            {
                logger.LogInformation("Database already contains users. Skipping seed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedTestUsersAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var testUsers = new[]
        {
            new
            {
                User = new ApplicationUser
                {
                    UserName = "testuser",
                    Email = "testuser@game.com",
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                },
                Password = "Password123!"
            },
            new
            {
                User = new ApplicationUser
                {
                    UserName = "player1",
                    Email = "player1@game.com",
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                },
                Password = "Player123!"
            },
            new
            {
                User = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@game.com",
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                },
                Password = "Admin123!"
            }
        };

        foreach (var testUser in testUsers)
        {
            var result = await userManager.CreateAsync(testUser.User, testUser.Password);

            if (result.Succeeded)
            {
                logger.LogInformation(
                    "✅ Created test user: {Username} / {Email}",
                    testUser.User.UserName,
                    testUser.User.Email);
            }
            else
            {
                logger.LogError(
                    "❌ Failed to create user {Username}: {Errors}",
                    testUser.User.UserName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
