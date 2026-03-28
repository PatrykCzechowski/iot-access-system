using AccessControl.Domain.Enums;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Persistence;

public class AdminSeeder(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync()
    {
        var email = configuration["Admin:Email"]
            ?? throw new InvalidOperationException(
                "Admin seed configuration missing: Admin:Email. Set via user secrets or environment variable Admin__Email.");
        var password = configuration["Admin:Password"]
            ?? throw new InvalidOperationException(
                "Admin seed configuration missing: Admin:Password. Set via user secrets or environment variable Admin__Password.");
        var fullName = configuration["Admin:FullName"] ?? "Administrator";

        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            logger.LogDebug("Admin account already exists ({Email}), skipping seed", existingAdmin.Email);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Role = UserRole.Admin,
            MustChangePassword = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin account: {errors}");
        }

        logger.LogInformation("Default admin account created ({Email}). Password change will be required on first login", email);
    }
}
