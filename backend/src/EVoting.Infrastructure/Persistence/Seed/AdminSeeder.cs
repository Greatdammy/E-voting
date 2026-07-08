using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EVoting.Infrastructure.Persistence.Seed;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        var alreadyHasAdmin = await context.Users.AnyAsync(u => u.Role == UserRole.Administrator);
        if (alreadyHasAdmin)
        {
            return;
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password not configured - skipping admin seed.");
            return;
        }

        var admin = new User
        {
            FullName = "Administrator",
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.Administrator,
            IsVerified = true
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded initial Administrator account ({Email}).", email);
    }
}
