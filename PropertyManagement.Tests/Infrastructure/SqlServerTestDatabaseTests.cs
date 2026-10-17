using Microsoft.EntityFrameworkCore;
using PropertyManagement.Tests.Infrastructure;

namespace PropertyManagement.Tests.Infrastructure;

public class SqlServerTestDatabaseTests
{
    [Fact]
    public async Task InitializeAsync_AppliesMigrationsToTestDatabase()
    {
        var database = new SqlServerTestDatabase();

        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var pendingMigrations =
            await context.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);

        var databaseName = await context.Database
            .SqlQueryRaw<string>("SELECT DB_NAME() AS [Value]")
            .SingleAsync();

        Assert.Equal("TroyPropertyManagement_Test", databaseName);
    }
}