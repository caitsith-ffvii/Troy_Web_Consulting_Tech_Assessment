using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Data;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Services;

public class RegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_Applicant_CreatesUserRoleAndApplicantProfile()
    {
        // Arrange
        var (context, service, userManager) = await CreateAsync();
        var email = UniqueEmail();

        // Act
        var result = await service.RegisterAsync(ApplicantRequest(email));

        // Assert
        Assert.True(result.Succeeded);

        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, Roles.Applicant));

        var applicant = await context.Applicants
            .SingleAsync(a => a.UserId == user.Id);

        Assert.Equal("Jane", applicant.FirstName);
        Assert.Equal("100 Main Street", applicant.CurrentAddress);
        Assert.False(await context.PropertyManagers
            .AnyAsync(m => m.UserId == user.Id));
    }

    [Fact]
    public async Task RegisterAsync_PropertyManager_CreatesUserRoleAndManagerProfile()
    {
        // Arrange
        var (context, service, userManager) = await CreateAsync();
        var email = UniqueEmail();

        // Act
        var result = await service.RegisterAsync(
            new RegistrationRequest(
                email,
                "Passw0rd!",
                Roles.PropertyManager,
                null,
                null,
                null,
                null));

        // Assert
        Assert.True(result.Succeeded);

        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, Roles.PropertyManager));
        Assert.True(await context.PropertyManagers
            .AnyAsync(m => m.UserId == user.Id));
        Assert.False(await context.Applicants
            .AnyAsync(a => a.UserId == user.Id));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_RejectsWithoutCreatingProfile()
    {
        // Arrange
        var (context, service, _) = await CreateAsync();
        var email = UniqueEmail();

        await service.RegisterAsync(ApplicantRequest(email));
        context.ChangeTracker.Clear();

        // Act
        var result = await service.RegisterAsync(ApplicantRequest(email));

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(1, await context.Users
            .CountAsync(u => u.Email == email));
        Assert.Equal(1, await context.Applicants
            .CountAsync(a => a.User.Email == email));
    }

    [Fact]
    public async Task RegisterAsync_ApplicantMissingRequiredFields_RejectsWithoutCreatingUser()
    {
        // Arrange
        var (context, service, _) = await CreateAsync();
        var email = UniqueEmail();

        // Act
        var result = await service.RegisterAsync(
            new RegistrationRequest(
                email,
                "Passw0rd!",
                Roles.Applicant,
                " ",
                "Applicant",
                "555-0100",
                "100 Main Street"));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "FirstName");
        Assert.False(await context.Users.AnyAsync(u => u.Email == email));
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_RejectsWithoutCreatingUser()
    {
        // Arrange
        var (context, service, _) = await CreateAsync();
        var email = UniqueEmail();

        // Act
        var result = await service.RegisterAsync(
            ApplicantRequest(email) with { Password = "short" });

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "Password");
        Assert.False(await context.Users.AnyAsync(u => u.Email == email));
    }

    [Fact]
    public async Task RegisterAsync_UnknownRole_RejectsWithoutCreatingUser()
    {
        // Arrange
        var (context, service, _) = await CreateAsync();
        var email = UniqueEmail();

        // Act
        var result = await service.RegisterAsync(
            ApplicantRequest(email) with { Role = "Admin" });

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(await context.Users.AnyAsync(u => u.Email == email));
    }

    private static async Task<(
        ApplicationDbContext Context,
        RegistrationService Service,
        UserManager<ApplicationUser> UserManager)> CreateAsync()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        var context = database.CreateContext();
        var provider = IdentityTestServices.Create(context);

        await IdentityTestServices.EnsureRolesAsync(
            provider.GetRequiredService<RoleManager<IdentityRole>>());

        var userManager =
            provider.GetRequiredService<UserManager<ApplicationUser>>();

        return (context, new RegistrationService(context, userManager), userManager);
    }

    private static string UniqueEmail() =>
        $"register-{Guid.NewGuid():N}@test.local";

    private static RegistrationRequest ApplicantRequest(string email) =>
        new(
            email,
            "Passw0rd!",
            Roles.Applicant,
            "Jane",
            "Applicant",
            "555-0100",
            "100 Main Street");
}
