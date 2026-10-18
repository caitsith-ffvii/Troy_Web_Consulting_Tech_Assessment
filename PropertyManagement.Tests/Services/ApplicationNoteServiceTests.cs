using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Data;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Services;

public class ApplicationNoteServiceTests
{
    [Fact]
    public async Task AddAsync_UnassignedManager_AddsNote()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var otherManager = await AddManagerAsync(context);
        var service = new ApplicationNoteService(context);

        // Act
        var result = await service.AddAsync(
            scenario.Application.Id,
            otherManager.Id,
            "  Called the landlord.  ");

        // Assert
        Assert.True(result.Succeeded);

        var note = await context.ApplicationNotes
            .SingleAsync(n => n.RentalApplicationId == scenario.Application.Id);

        Assert.Equal("Called the landlord.", note.Body);
        Assert.Equal(otherManager.Id, note.AuthorUserId);
    }

    [Fact]
    public async Task AddAsync_UserIsNotPropertyManager_Rejects()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var service = new ApplicationNoteService(context);

        // Act
        var result = await service.AddAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            "Should not be saved.");

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(await context.ApplicationNotes
            .AnyAsync(n => n.RentalApplicationId == scenario.Application.Id));
    }

    [Fact]
    public async Task ManagerWhoIsTheApplicant_CannotAddOrReadNotes()
    {
        // Arrange: the applicant's account also holds a manager profile.
        var (context, scenario) = await CreateAsync();

        context.PropertyManagers.Add(new PropertyManager
        {
            UserId = scenario.ApplicantUser.Id
        });

        await context.SaveChangesAsync();

        var service = new ApplicationNoteService(context);

        await service.AddAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            "Internal note.");

        // Act
        var addResult = await service.AddAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            "About my own application.");

        var notes = await service.GetNotesAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id);

        // Assert
        Assert.False(addResult.Succeeded);
        Assert.Null(notes);
        Assert.False(await service.CanViewNotesAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id));
    }

    [Fact]
    public async Task UpdateAsync_Author_UpdatesNoteAndEditTime()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var service = new ApplicationNoteService(context);

        await service.AddAsync(scenario.Application.Id, scenario.ManagerUser.Id, "First draft.");

        var note = await context.ApplicationNotes
            .SingleAsync(n => n.RentalApplicationId == scenario.Application.Id);

        // Act
        var result = await service.UpdateAsync(
            scenario.Application.Id,
            note.Id,
            scenario.ManagerUser.Id,
            "Revised.");

        // Assert
        Assert.True(result.Succeeded);

        context.ChangeTracker.Clear();

        var updated = await context.ApplicationNotes.SingleAsync(n => n.Id == note.Id);

        Assert.Equal("Revised.", updated.Body);
        Assert.True(updated.UpdatedAtUtc >= updated.CreatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_AnotherManager_RejectsWithoutChanges()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var otherManager = await AddManagerAsync(context);
        var service = new ApplicationNoteService(context);

        await service.AddAsync(scenario.Application.Id, scenario.ManagerUser.Id, "Original.");

        var note = await context.ApplicationNotes
            .SingleAsync(n => n.RentalApplicationId == scenario.Application.Id);

        // Act
        var result = await service.UpdateAsync(
            scenario.Application.Id,
            note.Id,
            otherManager.Id,
            "Overwritten.");

        // Assert
        Assert.False(result.Succeeded);

        context.ChangeTracker.Clear();

        Assert.Equal(
            "Original.",
            (await context.ApplicationNotes.SingleAsync(n => n.Id == note.Id)).Body);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AddAsync_BlankBody_ReturnsBodyError(string? body)
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var service = new ApplicationNoteService(context);

        // Act
        var result = await service.AddAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            body!);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "Body");
    }

    [Fact]
    public async Task AddAsync_BodyTooLong_ReturnsBodyError()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var service = new ApplicationNoteService(context);

        // Act
        var result = await service.AddAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            new string('x', 2001));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "Body");
    }

    [Fact]
    public async Task GetNotesAsync_MarksOnlyViewersOwnNotesEditable()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var otherManager = await AddManagerAsync(context);
        var service = new ApplicationNoteService(context);

        await service.AddAsync(scenario.Application.Id, scenario.ManagerUser.Id, "Mine.");
        await service.AddAsync(scenario.Application.Id, otherManager.Id, "Theirs.");

        // Act
        var notes = await service.GetNotesAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id);

        // Assert
        Assert.NotNull(notes);
        Assert.Equal(2, notes.Count);
        Assert.True(notes.Single(n => n.Body == "Mine.").CanEdit);
        Assert.False(notes.Single(n => n.Body == "Theirs.").CanEdit);
        Assert.Equal(otherManager.Email, notes.Single(n => n.Body == "Theirs.").AuthorEmail);
    }

    private static async Task<(ApplicationDbContext, SubmittedApplicationScenario)> CreateAsync()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        var context = database.CreateContext();
        var scenario = await SubmittedApplicationScenarioFactory.CreateAsync(context);

        return (context, scenario);
    }

    private static async Task<ApplicationUser> AddManagerAsync(ApplicationDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var user = new ApplicationUser
        {
            UserName = $"note-manager-{suffix}@test.local",
            Email = $"note-manager-{suffix}@test.local"
        };

        context.PropertyManagers.Add(new PropertyManager { User = user });
        await context.SaveChangesAsync();

        return user;
    }
}
