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
    
    [Fact]
    public async Task ChangeStatusAsync_TwoApplicationsApprovedConcurrently_CreatesOnlyOneLease()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        int unitId;
        int firstApplicationId;
        int secondApplicationId;
        string managerUserId;

        await using (var setupContext = database.CreateContext())
        await using (var transaction =
            await setupContext.Database.BeginTransactionAsync())
        {
            var firstScenario =
                await SubmittedApplicationScenarioFactory.CreateAsync(setupContext);

            var secondApplicantUser = new ApplicationUser
            {
                UserName = $"applicant-{Guid.NewGuid():N}@test.local",
                Email = $"applicant-{Guid.NewGuid():N}@test.local"
            };

            var secondApplicant = new Applicant
            {
                User = secondApplicantUser,
                FirstName = "Second",
                LastName = "Applicant",
                PhoneNumber = "555-0101",
                CurrentAddress = "300 Applicant Street"
            };

            var secondApplication = new RentalApplication
            {
                Applicant = secondApplicant,
                UnitId = firstScenario.Unit.Id,
                Status = RentalApplicationStatus.Submitted,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ApplicantInformation = new ApplicantInformationSnapshot
                {
                    FirstName = secondApplicant.FirstName,
                    LastName = secondApplicant.LastName,
                    PhoneNumber = secondApplicant.PhoneNumber,
                    Email = secondApplicantUser.Email!,
                    CurrentAddress = secondApplicant.CurrentAddress
                }
            };

            setupContext.Applicants.Add(secondApplicant);
            setupContext.RentalApplications.Add(secondApplication);

            await setupContext.SaveChangesAsync();

            unitId = firstScenario.Unit.Id;
            firstApplicationId = firstScenario.Application.Id;
            secondApplicationId = secondApplication.Id;
            managerUserId = firstScenario.ManagerUser.Id;

            await transaction.CommitAsync();
        }

        // Act
        async Task<ChangeApplicationStatusResult> ApproveAsync(int applicationId)
        {
            await using var context = database.CreateContext();
            var service = new RentalApplicationService(context);

            return await service.ChangeStatusAsync(
                applicationId,
                managerUserId,
                RentalApplicationStatus.Approved,
                null);
        }

        var results = await Task.WhenAll(
            ApproveAsync(firstApplicationId),
            ApproveAsync(secondApplicationId));

        // Assert
        Assert.Single(results, result => result.Succeeded);
        Assert.Single(results, result => !result.Succeeded);

        await using var assertContext = database.CreateContext();

        var leases = await assertContext.Leases
            .Where(lease => lease.UnitId == unitId)
            .ToListAsync();

        Assert.Single(leases);

        var applications = await assertContext.RentalApplications
            .Where(application =>
                application.Id == firstApplicationId ||
                application.Id == secondApplicationId)
            .ToListAsync();

        Assert.Single(
            applications,
            application => application.Status == RentalApplicationStatus.Approved);

        Assert.Single(
            applications,
            application => application.Status == RentalApplicationStatus.Submitted);
    }
    
    [Fact]
    public async Task CreateDraftAsync_ApplicantCreatesApplication_CreatesDraftWithInformationSnapshot()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var service = new RentalApplicationService(context);

        // Act
        var result = await service.CreateDraftAsync(
            scenario.ApplicantUser.Id,
            scenario.Unit.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Application);

        var application = await context.RentalApplications
            .Include(a => a.ApplicantInformation)
            .SingleAsync(a => a.Id == result.Application.Id);

        Assert.Equal(scenario.Applicant.Id, application.ApplicantId);
        Assert.Equal(scenario.Unit.Id, application.UnitId);
        Assert.Equal(RentalApplicationStatus.Draft, application.Status);

        Assert.Equal(
            scenario.Applicant.FirstName,
            application.ApplicantInformation.FirstName);

        Assert.Equal(
            scenario.Applicant.LastName,
            application.ApplicantInformation.LastName);

        Assert.Equal(
            scenario.Applicant.PhoneNumber,
            application.ApplicantInformation.PhoneNumber);

        Assert.Equal(
            scenario.ApplicantUser.Email,
            application.ApplicantInformation.Email);

        Assert.Equal(
            scenario.Applicant.CurrentAddress,
            application.ApplicantInformation.CurrentAddress);

        Assert.True(application.CreatedAtUtc > DateTime.MinValue);
        Assert.Equal(application.CreatedAtUtc, application.UpdatedAtUtc);
    }
    
    [Fact]
    public async Task CreateDraftAsync_UserHasNoApplicantProfile_RejectsOperation()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var service = new RentalApplicationService(context);
        var unknownUserId = $"unknown-{Guid.NewGuid():N}";

        // Act
        var result = await service.CreateDraftAsync(
            unknownUserId,
            scenario.Unit.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Application);
        Assert.NotNull(result.Error);

        Assert.Empty(await context.RentalApplications
            .Where(a => a.ApplicantId == scenario.Applicant.Id)
            .Where(a => a.UnitId == scenario.Unit.Id)
            .Where(a => a.Status == RentalApplicationStatus.Draft)
            .ToListAsync());
    }

    [Fact]
    public async Task CreateDraftAsync_UnitDoesNotExist_RejectsOperation()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var service = new RentalApplicationService(context);
        const int nonexistentUnitId = int.MaxValue;

        // Act
        var result = await service.CreateDraftAsync(
            scenario.ApplicantUser.Id,
            nonexistentUnitId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Application);
        Assert.NotNull(result.Error);

        Assert.Empty(await context.RentalApplications
            .Where(a => a.ApplicantId == scenario.Applicant.Id)
            .Where(a => a.UnitId == nonexistentUnitId)
            .ToListAsync());
    }
    
    [Fact]
    public async Task SubmitAsync_UnitHasActiveLease_RejectsSubmissionWithoutChanges()
    {
        // Arrange
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;
        await context.SaveChangesAsync();

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

        var service = new RentalApplicationService(context);

        // Act
        var result = await service.SubmitAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);

        var unchangedApplication = await context.RentalApplications
            .SingleAsync(a => a.Id == scenario.Application.Id);

        Assert.Equal(
            RentalApplicationStatus.Draft,
            unchangedApplication.Status);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h => h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task SubmitAsync_ValidDraft_SubmitsApplicationAndRecordsHistory()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.SubmitAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id);

        Assert.True(result.Succeeded);

        var history = await context.ApplicationStatusHistories
            .SingleAsync(h =>
                h.RentalApplicationId == scenario.Application.Id);

        Assert.Equal(RentalApplicationStatus.Draft, history.PreviousStatus);
        Assert.Equal(RentalApplicationStatus.Submitted, history.NewStatus);
        Assert.Equal(scenario.ApplicantUser.Id, history.ChangedByUserId);

        Assert.Equal(
            RentalApplicationStatus.Submitted,
            scenario.Application.Status);
    }

    [Fact]
    public async Task SubmitAsync_AnotherApplicant_SubmissionIsRejected()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var service = new RentalApplicationService(context);

        var result = await service.SubmitAsync(
            scenario.Application.Id,
            Guid.NewGuid().ToString());

        Assert.False(result.Succeeded);
        Assert.Equal(
            RentalApplicationStatus.Submitted,
            scenario.Application.Status);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h => h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task SubmitAsync_ApprovedApplication_SubmissionIsRejected()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Approved;
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.SubmitAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(
            RentalApplicationStatus.Approved,
            scenario.Application.Status);

        Assert.Empty(await context.ApplicationStatusHistories
            .Where(h => h.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }
    
    [Fact]
    public async Task SaveApplicantInformationAsync_OwnDraft_UpdatesInformation()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.SaveApplicantInformationAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            new ApplicantInformationSnapshot
            {
                FirstName = "Updated",
                LastName = "Applicant",
                PhoneNumber = "555-0100",
                Email = "updated@example.com",
                CurrentAddress = "123 New Street"
            });

        Assert.True(result.Succeeded);

        var savedInformation = await context.ApplicantInformationSnapshots
            .SingleAsync(i =>
                i.RentalApplicationId == scenario.Application.Id);

        Assert.Equal("Updated", savedInformation.FirstName);
        Assert.Equal("Applicant", savedInformation.LastName);
        Assert.Equal("555-0100", savedInformation.PhoneNumber);
        Assert.Equal("updated@example.com", savedInformation.Email);
        Assert.Equal("123 New Street", savedInformation.CurrentAddress);
    }

    [Fact]
    public async Task SaveApplicantInformationAsync_DifferentApplicant_RejectsUpdate()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;
        await context.SaveChangesAsync();

        var originalFirstName =
            scenario.Application.ApplicantInformation.FirstName;

        var service = new RentalApplicationService(context);

        var result = await service.SaveApplicantInformationAsync(
            scenario.Application.Id,
            Guid.NewGuid().ToString(),
            new ApplicantInformationSnapshot
            {
                FirstName = "Unauthorized",
                LastName = "Change",
                PhoneNumber = "555-0199",
                Email = "unauthorized@example.com",
                CurrentAddress = "999 Unknown Street"
            });

        Assert.False(result.Succeeded);
        Assert.Equal(
            originalFirstName,
            scenario.Application.ApplicantInformation.FirstName);
    }

    [Fact]
    public async Task SaveApplicantInformationAsync_SubmittedApplication_RejectsUpdate()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var originalFirstName =
            scenario.Application.ApplicantInformation.FirstName;

        var service = new RentalApplicationService(context);

        var result = await service.SaveApplicantInformationAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            new ApplicantInformationSnapshot
            {
                FirstName = "Too Late",
                LastName = "Change",
                PhoneNumber = "555-0199",
                Email = "late@example.com",
                CurrentAddress = "999 Unknown Street"
            });

        Assert.False(result.Succeeded);
        Assert.Equal(
            originalFirstName,
            scenario.Application.ApplicantInformation.FirstName);
    }

    [Fact]
    public async Task AddResidenceHistoryAsync_OwnDraft_AddsEntry()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var entry = new ResidenceHistoryEntry
        {
            Address = "123 Previous Street",
            LandlordName = "Jordan Landlord",
            LandlordPhoneNumber = "555-0123",
            MoveInDate = new DateTime(2022, 1, 1),
            MoveOutDate = new DateTime(2024, 1, 1)
        };

        var result = await service.AddResidenceHistoryAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            entry);

        Assert.True(result.Succeeded);

        var savedEntry = await context.ResidenceHistoryEntries
            .SingleAsync(e => e.RentalApplicationId == scenario.Application.Id);

        Assert.Equal("123 Previous Street", savedEntry.Address);
        Assert.Equal("Jordan Landlord", savedEntry.LandlordName);
        Assert.Equal("555-0123", savedEntry.LandlordPhoneNumber);
    }

    [Fact]
    public async Task AddResidenceHistoryAsync_DifferentApplicant_RejectsEntry()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.AddResidenceHistoryAsync(
            scenario.Application.Id,
            Guid.NewGuid().ToString(),
            new ResidenceHistoryEntry
            {
                Address = "123 Unauthorized Street",
                LandlordName = "Unauthorized Landlord",
                LandlordPhoneNumber = "555-0199",
                MoveInDate = new DateTime(2022, 1, 1),
                MoveOutDate = new DateTime(2024, 1, 1)
            });

        Assert.False(result.Succeeded);

        Assert.Empty(await context.ResidenceHistoryEntries
            .Where(e => e.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task AddResidenceHistoryAsync_SubmittedApplication_RejectsEntry()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var service = new RentalApplicationService(context);

        var result = await service.AddResidenceHistoryAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            new ResidenceHistoryEntry
            {
                Address = "123 Too Late Street",
                LandlordName = "Landlord",
                LandlordPhoneNumber = "555-0199",
                MoveInDate = new DateTime(2022, 1, 1),
                MoveOutDate = new DateTime(2024, 1, 1)
            });

        Assert.False(result.Succeeded);

        Assert.Empty(await context.ResidenceHistoryEntries
            .Where(e => e.RentalApplicationId == scenario.Application.Id)
            .ToListAsync());
    }
    
    [Fact]
    public async Task UpdateResidenceHistoryAsync_OwnDraft_UpdatesEntry()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;

        var entry = new ResidenceHistoryEntry
        {
            Address = "123 Old Street",
            LandlordName = "Old Landlord",
            LandlordPhoneNumber = "555-0100",
            MoveInDate = new DateTime(2022, 1, 1),
            MoveOutDate = new DateTime(2024, 1, 1)
        };

        scenario.Application.ResidenceHistory.Add(entry);
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.UpdateResidenceHistoryAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            entry.Id,
            new ResidenceHistoryEntry
            {
                Address = "456 New Street",
                LandlordName = "New Landlord",
                LandlordPhoneNumber = "555-0123",
                MoveInDate = new DateTime(2023, 1, 1),
                MoveOutDate = new DateTime(2025, 1, 1)
            });

        Assert.True(result.Succeeded);

        var savedEntry = await context.ResidenceHistoryEntries
            .SingleAsync(e => e.Id == entry.Id);

        Assert.Equal("456 New Street", savedEntry.Address);
        Assert.Equal("New Landlord", savedEntry.LandlordName);
        Assert.Equal("555-0123", savedEntry.LandlordPhoneNumber);
        Assert.Equal(new DateTime(2023, 1, 1), savedEntry.MoveInDate);
        Assert.Equal(new DateTime(2025, 1, 1), savedEntry.MoveOutDate);
    }

    [Fact]
    public async Task UpdateResidenceHistoryAsync_DifferentApplicant_RejectsUpdate()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;

        var entry = new ResidenceHistoryEntry
        {
            Address = "123 Original Street",
            LandlordName = "Original Landlord",
            LandlordPhoneNumber = "555-0100",
            MoveInDate = new DateTime(2022, 1, 1),
            MoveOutDate = new DateTime(2024, 1, 1)
        };

        scenario.Application.ResidenceHistory.Add(entry);
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.UpdateResidenceHistoryAsync(
            scenario.Application.Id,
            Guid.NewGuid().ToString(),
            entry.Id,
            new ResidenceHistoryEntry
            {
                Address = "Unauthorized Street",
                LandlordName = "Unauthorized Landlord",
                LandlordPhoneNumber = "555-0199",
                MoveInDate = new DateTime(2023, 1, 1),
                MoveOutDate = new DateTime(2025, 1, 1)
            });

        Assert.False(result.Succeeded);

        var savedEntry = await context.ResidenceHistoryEntries
            .SingleAsync(e => e.Id == entry.Id);

        Assert.Equal("123 Original Street", savedEntry.Address);
    }

    [Fact]
    public async Task UpdateResidenceHistoryAsync_SubmittedApplication_RejectsUpdate()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var entry = new ResidenceHistoryEntry
        {
            Address = "123 Original Street",
            LandlordName = "Original Landlord",
            LandlordPhoneNumber = "555-0100",
            MoveInDate = new DateTime(2022, 1, 1),
            MoveOutDate = new DateTime(2024, 1, 1)
        };

        scenario.Application.ResidenceHistory.Add(entry);
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.UpdateResidenceHistoryAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            entry.Id,
            new ResidenceHistoryEntry
            {
                Address = "Too Late Street",
                LandlordName = "Different Landlord",
                LandlordPhoneNumber = "555-0199",
                MoveInDate = new DateTime(2023, 1, 1),
                MoveOutDate = new DateTime(2025, 1, 1)
            });

        Assert.False(result.Succeeded);

        var savedEntry = await context.ResidenceHistoryEntries
            .SingleAsync(e => e.Id == entry.Id);

        Assert.Equal("123 Original Street", savedEntry.Address);
    }

    [Fact]
    public async Task RemoveResidenceHistoryAsync_OwnDraft_RemovesEntry()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;

        var entry = new ResidenceHistoryEntry
        {
            Address = "123 Old Street",
            LandlordName = "Old Landlord",
            LandlordPhoneNumber = "555-0100",
            MoveInDate = new DateTime(2022, 1, 1),
            MoveOutDate = new DateTime(2024, 1, 1)
        };

        scenario.Application.ResidenceHistory.Add(entry);
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.RemoveResidenceHistoryAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            entry.Id);

        Assert.True(result.Succeeded);

        var savedEntry = await context.ResidenceHistoryEntries
            .SingleOrDefaultAsync(e => e.Id == entry.Id);

        Assert.Null(savedEntry);
    }

    [Fact]
    public async Task RemoveResidenceHistoryAsync_DifferentApplicant_RejectsRemoval()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.Status = RentalApplicationStatus.Draft;

        var entry = new ResidenceHistoryEntry
        {
            Address = "123 Original Street",
            LandlordName = "Original Landlord",
            LandlordPhoneNumber = "555-0100",
            MoveInDate = new DateTime(2022, 1, 1),
            MoveOutDate = new DateTime(2024, 1, 1)
        };

        scenario.Application.ResidenceHistory.Add(entry);
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.RemoveResidenceHistoryAsync(
            scenario.Application.Id,
            Guid.NewGuid().ToString(),
            entry.Id);

        Assert.False(result.Succeeded);

        var savedEntry = await context.ResidenceHistoryEntries
            .SingleOrDefaultAsync(e => e.Id == entry.Id);

        Assert.NotNull(savedEntry);
    }

    [Fact]
    public async Task RemoveResidenceHistoryAsync_SubmittedApplication_RejectsRemoval()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();

        var scenario =
            await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var entry = new ResidenceHistoryEntry
        {
            Address = "123 Original Street",
            LandlordName = "Original Landlord",
            LandlordPhoneNumber = "555-0100",
            MoveInDate = new DateTime(2022, 1, 1),
            MoveOutDate = new DateTime(2024, 1, 1)
        };

        scenario.Application.ResidenceHistory.Add(entry);
        await context.SaveChangesAsync();

        var service = new RentalApplicationService(context);

        var result = await service.RemoveResidenceHistoryAsync(
            scenario.Application.Id,
            scenario.ApplicantUser.Id,
            entry.Id);

        Assert.False(result.Succeeded);

        var savedEntry = await context.ResidenceHistoryEntries
            .SingleOrDefaultAsync(e => e.Id == entry.Id);

        Assert.NotNull(savedEntry);
    }

}