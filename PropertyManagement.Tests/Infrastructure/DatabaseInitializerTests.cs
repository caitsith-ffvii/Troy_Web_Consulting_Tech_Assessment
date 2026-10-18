using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Tests.Infrastructure;

// Tests in one class run sequentially, so full seeding never races itself.
public class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_RunTwice_CreatesEachRoleOnce()
    {
        // Arrange
        var (context, initializer) = await CreateAsync();

        // Act
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        // Assert
        Assert.Equal(1, await context.Roles
            .CountAsync(r => r.Name == Roles.Applicant));
        Assert.Equal(1, await context.Roles
            .CountAsync(r => r.Name == Roles.PropertyManager));
    }

    [Fact]
    public async Task InitializeAsync_RunTwice_SeedsDemoDataOnce()
    {
        // Arrange
        var (context, initializer) = await CreateAsync();

        // Act
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        // Assert
        foreach (var email in DemoDataSeeder.ManagerEmails
                     .Concat(DemoDataSeeder.ApplicantEmails))
        {
            Assert.Equal(1, await context.Users
                .CountAsync(u => u.Email == email));
        }

        foreach (var name in DemoDataSeeder.PropertyNames)
        {
            Assert.Equal(1, await context.Properties
                .CountAsync(p => p.Name == name));
        }
    }

    [Fact]
    public async Task InitializeAsync_SeedsApplicationsInEveryStatus()
    {
        // Arrange
        var (context, initializer) = await CreateAsync();

        // Act
        await initializer.InitializeAsync();

        // Assert
        var seededStatuses = await SeededApplications(context)
            .Select(a => a.Status)
            .Distinct()
            .ToListAsync();

        Assert.Equal(
            Enum.GetValues<RentalApplicationStatus>().Order(),
            seededStatuses.Order());
    }

    [Fact]
    public async Task InitializeAsync_SeededData_SatisfiesDomainInvariants()
    {
        // Arrange
        var (context, initializer) = await CreateAsync();

        // Act
        await initializer.InitializeAsync();

        // Assert
        var applications = await SeededApplications(context)
            .Include(a => a.StatusHistory)
            .ToListAsync();

        foreach (var application in applications)
        {
            var history = application.StatusHistory
                .OrderBy(h => h.ChangedAtUtc)
                .ToList();

            if (application.Status == RentalApplicationStatus.Draft)
            {
                Assert.Empty(history);
                continue;
            }

            Assert.True(application.ApplicantInformationCompleted);
            Assert.True(application.ResidenceHistoryCompleted);
            Assert.Equal(application.Status, history[^1].NewStatus);

            if (application.Status is RentalApplicationStatus.Returned
                or RentalApplicationStatus.Denied)
            {
                Assert.False(string.IsNullOrWhiteSpace(history[^1].Comment));
            }

            if (application.Status == RentalApplicationStatus.Approved)
            {
                Assert.True(await context.Leases.AnyAsync(l =>
                    l.UnitId == application.UnitId &&
                    l.ApplicantId == application.ApplicantId));
            }
        }

        var today = DateTime.UtcNow.Date;

        var unitsWithOverlappingActiveLeases = await context.Leases
            .Where(l =>
                DemoDataSeeder.PropertyNames.Contains(l.Unit.Property.Name) &&
                l.StartDate <= today &&
                l.EndDate >= today)
            .GroupBy(l => l.UnitId)
            .CountAsync(g => g.Count() > 1);

        Assert.Equal(0, unitsWithOverlappingActiveLeases);
    }

    private static IQueryable<RentalApplication> SeededApplications(
        ApplicationDbContext context) =>
        context.RentalApplications
            .Where(a =>
                DemoDataSeeder.PropertyNames.Contains(a.Unit.Property.Name));

    private static async Task<(ApplicationDbContext, DatabaseInitializer)>
        CreateAsync()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        var context = database.CreateContext();
        var provider = IdentityTestServices.Create(context);

        var initializer = new DatabaseInitializer(
            context,
            provider.GetRequiredService<RoleManager<IdentityRole>>(),
            provider.GetRequiredService<UserManager<ApplicationUser>>());

        return (context, initializer);
    }
}
