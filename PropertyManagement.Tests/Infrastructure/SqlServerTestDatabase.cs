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

    // Tests run in parallel; without this, several tests can try to create
    // a missing test database at the same time and collide.
    private static readonly SemaphoreSlim MigrationLock = new(1, 1);
    private static volatile bool migrated;

    public async Task InitializeAsync()
    {
        if (migrated)
        {
            return;
        }

        await MigrationLock.WaitAsync();

        try
        {
            if (!migrated)
            {
                await using var context = CreateContext();
                await context.Database.MigrateAsync();
                migrated = true;
            }
        }
        finally
        {
            MigrationLock.Release();
        }
    }
}