using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Tests.Infrastructure;

public sealed record SubmittedApplicationScenario(
    ApplicationUser ManagerUser,
    PropertyManager Manager,
    ApplicationUser ApplicantUser,
    Applicant Applicant,
    Unit Unit,
    RentalApplication Application);

public static class SubmittedApplicationScenarioFactory
{
    public static async Task<SubmittedApplicationScenario> CreateAsync(
        ApplicationDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        var managerUser = new ApplicationUser
        {
            UserName = $"manager-{suffix}@test.local",
            Email = $"manager-{suffix}@test.local"
        };

        var applicantUser = new ApplicationUser
        {
            UserName = $"applicant-{suffix}@test.local",
            Email = $"applicant-{suffix}@test.local"
        };

        var manager = new PropertyManager
        {
            User = managerUser
        };

        var applicant = new Applicant
        {
            User = applicantUser,
            FirstName = "Test",
            LastName = "Applicant",
            PhoneNumber = "(555) 555-0100",
            CurrentAddress = "100 Applicant Street"
        };

        var unitType = new UnitType
        {
            Name = $"Test Unit Type {suffix}",
            IsActive = true
        };

        var property = new Property
        {
            PropertyManager = manager,
            Name = $"Test Property {suffix}",
            Address = "200 Property Street",
            IsArchived = false
        };

        var unit = new Unit
        {
            Property = property,
            UnitType = unitType,
            UnitNumber = "1A",
            MonthlyRent = 1200m,
            IsArchived = false
        };

        var application = new RentalApplication
        {
            Applicant = applicant,
            Unit = unit,
            Status = RentalApplicationStatus.Submitted,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ApplicantInformation = new ApplicantInformationSnapshot
            {
                FirstName = applicant.FirstName,
                LastName = applicant.LastName,
                PhoneNumber = applicant.PhoneNumber,
                Email = applicantUser.Email!,
                CurrentAddress = applicant.CurrentAddress
            }
        };

        context.PropertyManagers.Add(manager);
        context.Applicants.Add(applicant);
        context.UnitTypes.Add(unitType);
        context.Properties.Add(property);
        context.Units.Add(unit);
        context.RentalApplications.Add(application);

        await context.SaveChangesAsync();

        return new SubmittedApplicationScenario(
            managerUser,
            manager,
            applicantUser,
            applicant,
            unit,
            application);
    }
}