using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Tests.Infrastructure;

public sealed class SqlServerTestDatabase
{
    public ApplicationDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Test database connection string is missing.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }
}