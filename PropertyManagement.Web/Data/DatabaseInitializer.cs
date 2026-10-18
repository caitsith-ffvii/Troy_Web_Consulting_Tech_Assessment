using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Data;

public class DatabaseInitializer(
    ApplicationDbContext context,
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager)
{
    public async Task InitializeAsync()
    {
        await context.Database.MigrateAsync();
        await EnsureRolesAsync();

        await new DemoDataSeeder(
                context,
                new RegistrationService(context, userManager))
            .SeedAsync();
    }

    public async Task EnsureRolesAsync()
    {
        foreach (var role in new[] { Roles.Applicant, Roles.PropertyManager })
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));

            // Startup must fail visibly rather than run without roles.
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{role}'.");
            }
        }
    }
}
