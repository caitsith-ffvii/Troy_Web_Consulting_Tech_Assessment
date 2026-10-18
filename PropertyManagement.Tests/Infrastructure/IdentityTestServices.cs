using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Tests.Infrastructure;

public static class IdentityTestServices
{
    // Identity stores share the test's DbContext, so a service transaction
    // covers both Identity writes and domain-profile writes.
    public static ServiceProvider Create(ApplicationDbContext context)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(context);

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        return services.BuildServiceProvider();
    }
}
