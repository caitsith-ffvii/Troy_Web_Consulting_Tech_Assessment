using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Identity;

namespace PropertyManagement.Web.Data;

public class DatabaseInitializer(
    ApplicationDbContext context,
    RoleManager<IdentityRole> roleManager)
{
    public async Task InitializeAsync()
    {
        await context.Database.MigrateAsync();
        await EnsureRolesAsync();
    }

    private async Task EnsureRolesAsync()
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
