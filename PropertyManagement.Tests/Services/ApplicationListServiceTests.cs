using PropertyManagement.Domain.Entities;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Data;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Services;

// The shared test database holds other tests' data, so each test scopes
// its assertions to its own scenario's property or applicant.
public class ApplicationListServiceTests
{
    private static readonly DateTime BaseTime =
        new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetApplicantApplicationsAsync_ReturnsOnlyOwnApplications()
    {
        // Arrange
        var (context, mine) = await CreateAsync();
        var other = await SubmittedApplicationScenarioFactory.CreateAsync(context);
        var service = new ApplicationListService(context);

        // Act
        var result = await service.GetApplicantApplicationsAsync(
            mine.ApplicantUser.Id,
            Query(status: null, propertyId: null));

        // Assert
        Assert.Contains(result.Items, a => a.Id == mine.Application.Id);
        Assert.DoesNotContain(result.Items, a => a.Id == other.Application.Id);
        Assert.All(result.Items, a => Assert.Equal(mine.Applicant.Id, a.ApplicantId));
    }

    [Fact]
    public async Task GetManagerApplicationsAsync_FiltersByStatusAndProperty()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();

        await AddApplicationAsync(context, scenario, RentalApplicationStatus.Submitted, BaseTime);
        await AddApplicationAsync(context, scenario, RentalApplicationStatus.Draft, null);
        await AddApplicationAsync(context, scenario, RentalApplicationStatus.Approved, BaseTime);

        var otherProperty = await SubmittedApplicationScenarioFactory.CreateAsync(context);
        var service = new ApplicationListService(context);

        // Act
        var result = await service.GetManagerApplicationsAsync(
            otherProperty.ManagerUser.Id,
            Query(RentalApplicationStatus.Submitted, scenario.Unit.PropertyId));

        // Assert: the scenario's own application plus the one added above.
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, a =>
        {
            Assert.Equal(RentalApplicationStatus.Submitted, a.Status);
            Assert.Equal(scenario.Unit.PropertyId, a.Unit.PropertyId);
        });
    }

    [Fact]
    public async Task GetManagerApplicationsAsync_DefaultSort_OldestSubmissionFirstWithIdTieBreaker()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();

        var newest = await AddApplicationAsync(
            context, scenario, RentalApplicationStatus.Submitted, BaseTime.AddDays(2));
        var tiedFirst = await AddApplicationAsync(
            context, scenario, RentalApplicationStatus.Submitted, BaseTime.AddDays(1));
        var tiedSecond = await AddApplicationAsync(
            context, scenario, RentalApplicationStatus.Submitted, BaseTime.AddDays(1));

        var service = new ApplicationListService(context);

        // Act
        var result = await service.GetManagerApplicationsAsync(
            scenario.ManagerUser.Id,
            Query(RentalApplicationStatus.Submitted, scenario.Unit.PropertyId));

        // Assert: the scenario's application was submitted earliest.
        Assert.Equal(
            [scenario.Application.Id, tiedFirst.Id, tiedSecond.Id, newest.Id],
            result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetManagerApplicationsAsync_SecondPage_ReturnsThatPageAndTotals()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var added = new List<RentalApplication>();

        for (var i = 1; i <= 4; i++)
        {
            added.Add(await AddApplicationAsync(
                context, scenario, RentalApplicationStatus.Submitted, BaseTime.AddDays(i)));
        }

        var service = new ApplicationListService(context);

        // Act
        var result = await service.GetManagerApplicationsAsync(
            scenario.ManagerUser.Id,
            Query(RentalApplicationStatus.Submitted, scenario.Unit.PropertyId) with
            {
                PageNumber = 2,
                PageSize = 2
            });

        // Assert: 5 in total (the scenario's plus 4); page 2 holds the 3rd and 4th.
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal([added[1].Id, added[2].Id], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetManagerApplicationsAsync_PageBeyondLastPage_ReturnsLastPage()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var service = new ApplicationListService(context);

        // Act
        var result = await service.GetManagerApplicationsAsync(
            scenario.ManagerUser.Id,
            Query(RentalApplicationStatus.Submitted, scenario.Unit.PropertyId) with
            {
                PageNumber = 99
            });

        // Assert
        Assert.Equal(1, result.PageNumber);
        Assert.Contains(result.Items, a => a.Id == scenario.Application.Id);
    }

    [Fact]
    public async Task GetManagerApplicationsAsync_UserIsNotPropertyManager_ReturnsNothing()
    {
        // Arrange
        var (context, scenario) = await CreateAsync();
        var service = new ApplicationListService(context);

        // Act
        var result = await service.GetManagerApplicationsAsync(
            scenario.ApplicantUser.Id,
            Query(status: null, propertyId: null));

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    private static ApplicationListQuery Query(
        RentalApplicationStatus? status,
        int? propertyId) =>
        new(status, propertyId, ApplicationSortField.Submitted, Descending: false, PageNumber: 1);

    private static async Task<(ApplicationDbContext, SubmittedApplicationScenario)> CreateAsync()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        var context = database.CreateContext();
        var scenario = await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.SubmittedAtUtc = BaseTime;
        await context.SaveChangesAsync();

        return (context, scenario);
    }

    private static async Task<RentalApplication> AddApplicationAsync(
        ApplicationDbContext context,
        SubmittedApplicationScenario scenario,
        RentalApplicationStatus status,
        DateTime? submittedAtUtc)
    {
        var application = new RentalApplication
        {
            ApplicantId = scenario.Applicant.Id,
            UnitId = scenario.Unit.Id,
            Status = status,
            CreatedAtUtc = BaseTime,
            UpdatedAtUtc = BaseTime,
            SubmittedAtUtc = submittedAtUtc,
            ApplicantInformation = new ApplicantInformationSnapshot
            {
                FirstName = scenario.Applicant.FirstName,
                LastName = scenario.Applicant.LastName,
                PhoneNumber = scenario.Applicant.PhoneNumber,
                Email = scenario.ApplicantUser.Email!,
                CurrentAddress = scenario.Applicant.CurrentAddress
            }
        };

        context.RentalApplications.Add(application);
        await context.SaveChangesAsync();

        return application;
    }
}
