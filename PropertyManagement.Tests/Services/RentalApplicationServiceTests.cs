using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Services;

public class RentalApplicationServiceTests
{
    [Fact]
    public async Task ChangeStatusAsync_ManagerReturnsApplicationWithComment_UpdatesStatusAndRecordsHistory()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var originalUpdatedAt = scenario.Application.UpdatedAtUtc;
        const string comment =
            "Please provide additional residence history.";

        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            RentalApplicationStatus.Returned,
            comment);

        // Assert
        Assert.True(result.Succeeded);

        var updatedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Returned,
            updatedApplication.Status);

        Assert.True(updatedApplication.UpdatedAtUtc >= originalUpdatedAt);

        var history = await context.ApplicationStatusHistories
            .SingleAsync(h =>
                h.RentalApplicationId == scenario.Application.Id);

        Assert.Equal(scenario.ManagerUser.Id, history.ChangedByUserId);
        Assert.Equal(
            RentalApplicationStatus.Submitted,
            history.PreviousStatus);
        Assert.Equal(
            RentalApplicationStatus.Returned,
            history.NewStatus);
        Assert.Equal(comment, history.Comment);
        Assert.True(history.ChangedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public async Task ChangeStatusAsync_ReturnsApplicationWithoutComment_RejectsOperationWithoutChanges()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var originalUpdatedAt = scenario.Application.UpdatedAtUtc;
        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            RentalApplicationStatus.Returned,
            null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        var unchangedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            unchangedApplication.Status);

        Assert.Equal(
            originalUpdatedAt,
            unchangedApplication.UpdatedAtUtc);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h =>
                h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task ChangeStatusAsync_ManagerDoesNotOwnProperty_RejectsOperation()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var suffix = Guid.NewGuid().ToString("N");

        var otherManagerUser = new ApplicationUser
        {
            UserName = $"other-manager-{suffix}@test.local",
            Email = $"other-manager-{suffix}@test.local"
        };

        var otherManager = new PropertyManager
        {
            User = otherManagerUser
        };

        context.PropertyManagers.Add(otherManager);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            otherManagerUser.Id,
            RentalApplicationStatus.Returned,
            "Please provide additional residence history.");

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        var unchangedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            unchangedApplication.Status);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h =>
                h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

        [Fact]
    public async Task ChangeStatusAsync_ApplicationDoesNotExist_RejectsOperation()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var service = new RentalApplicationService(context);
        const int nonexistentApplicationId = int.MaxValue;
        const string managerUserId = "nonexistent-manager";

        // Act
        var result = await service.ChangeStatusAsync(
            nonexistentApplicationId,
            managerUserId,
            RentalApplicationStatus.Returned,
            "Please provide additional residence history.");

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h =>
                h.RentalApplicationId == nonexistentApplicationId)
            .ToListAsync());
    }

    [Fact]
    public async Task ChangeStatusAsync_DeniesApplicationWithoutComment_RejectsOperationWithoutChanges()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var originalUpdatedAt = scenario.Application.UpdatedAtUtc;
        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            RentalApplicationStatus.Denied,
            null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        var unchangedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            unchangedApplication.Status);

        Assert.Equal(
            originalUpdatedAt,
            unchangedApplication.UpdatedAtUtc);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h =>
                h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task ChangeStatusAsync_ManagerDeniesApplicationWithComment_UpdatesStatusAndRecordsHistory()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var originalUpdatedAt = scenario.Application.UpdatedAtUtc;
        const string comment = "Application does not meet requirements.";

        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            RentalApplicationStatus.Denied,
            comment);

        // Assert
        Assert.True(result.Succeeded);

        var updatedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Denied,
            updatedApplication.Status);

        Assert.True(updatedApplication.UpdatedAtUtc >= originalUpdatedAt);

        var history = await context.ApplicationStatusHistories
            .SingleAsync(h =>
                h.RentalApplicationId == scenario.Application.Id);

        Assert.Equal(scenario.ManagerUser.Id, history.ChangedByUserId);
        Assert.Equal(
            RentalApplicationStatus.Submitted,
            history.PreviousStatus);
        Assert.Equal(
            RentalApplicationStatus.Denied,
            history.NewStatus);
        Assert.Equal(comment, history.Comment);
        Assert.True(history.ChangedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidStatusTransition_RejectsOperationWithoutChanges()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var originalUpdatedAt = scenario.Application.UpdatedAtUtc;
        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            RentalApplicationStatus.Draft,
            null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        var unchangedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            unchangedApplication.Status);

        Assert.Equal(
            originalUpdatedAt,
            unchangedApplication.UpdatedAtUtc);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h =>
                h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

        [Theory]
    [InlineData(RentalApplicationStatus.Returned)]
    [InlineData(RentalApplicationStatus.Denied)]
    public async Task ChangeStatusAsync_RequiresNonWhitespaceComment_RejectsOperationWithoutChanges(
        RentalApplicationStatus newStatus)
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var originalUpdatedAt = scenario.Application.UpdatedAtUtc;
        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            newStatus,
            "   ");

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        var unchangedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            unchangedApplication.Status);

        Assert.Equal(
            originalUpdatedAt,
            unchangedApplication.UpdatedAtUtc);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h =>
                h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task ChangeStatusAsync_ManagerApprovesApplication_CreatesLeaseAndRecordsHistory()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            RentalApplicationStatus.Approved,
            null);

        // Assert
        Assert.True(result.Succeeded);

        var approvedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Approved,
            approvedApplication.Status);

        var lease = await context.Leases
            .SingleAsync(l => l.UnitId == scenario.Unit.Id);

        Assert.Equal(scenario.Applicant.Id, lease.ApplicantId);
        Assert.Equal(scenario.Unit.Id, lease.UnitId);
        Assert.Equal(scenario.Unit.MonthlyRent, lease.AgreedRent);
        Assert.Equal(DateTime.UtcNow.Date, lease.StartDate.Date);
        Assert.Equal(
            lease.StartDate.Date.AddMonths(12),
            lease.EndDate.Date);

        var history = await context.ApplicationStatusHistories
            .SingleAsync(h =>
                h.RentalApplicationId == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            history.PreviousStatus);
        Assert.Equal(
            RentalApplicationStatus.Approved,
            history.NewStatus);
        Assert.Equal(scenario.ManagerUser.Id, history.ChangedByUserId);
    }

    [Fact]
    public async Task ChangeStatusAsync_UnitHasActiveLease_RejectsApprovalWithoutChanges()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var today = DateTime.UtcNow.Date;

        context.Leases.Add(new Lease
        {
            UnitId = scenario.Unit.Id,
            ApplicantId = scenario.Applicant.Id,
            StartDate = today.AddMonths(-1),
            EndDate = today.AddMonths(11),
            AgreedRent = scenario.Unit.MonthlyRent
        });

        await context.SaveChangesAsync();

        var originalUpdatedAt = scenario.Application.UpdatedAtUtc;
        var service = new RentalApplicationService(context);

        // Act
        var result = await service.ChangeStatusAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            RentalApplicationStatus.Approved,
            null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        var unchangedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            unchangedApplication.Status);

        Assert.Equal(
            originalUpdatedAt,
            unchangedApplication.UpdatedAtUtc);

        var leases = await context.Leases
            .Where(l => l.UnitId == scenario.Unit.Id)
            .ToListAsync();

        Assert.Single(leases);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h =>
                h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }
}