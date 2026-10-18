using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Tests.Infrastructure;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_RunTwice_CreatesEachRoleOnce()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var provider = IdentityTestServices.Create(context);

        var initializer = new DatabaseInitializer(
            context,
            provider.GetRequiredService<RoleManager<IdentityRole>>());

        // Act
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        // Assert
        Assert.Equal(1, await context.Roles
            .CountAsync(r => r.Name == Roles.Applicant));
        Assert.Equal(1, await context.Roles
            .CountAsync(r => r.Name == Roles.PropertyManager));
    }
}
