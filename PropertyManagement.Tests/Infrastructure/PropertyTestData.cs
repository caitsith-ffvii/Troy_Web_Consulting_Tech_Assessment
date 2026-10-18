using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Tests.Infrastructure;

public sealed record ManagedPropertyScenario(
    ApplicationUser ManagerUser,
    ApplicationUser OtherManagerUser,
    Property Property,
    Unit Unit,
    UnitType ActiveUnitType,
    UnitType InactiveUnitType);

public static class PropertyTestData
{
    // A property with one unit, its assigned manager, a second unassigned
    // manager, and one active and one inactive unit type.
    public static async Task<ManagedPropertyScenario> CreateAsync(
        ApplicationDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var managerUser = NewUser($"manager-{suffix}");
        var otherManagerUser = NewUser($"other-manager-{suffix}");

        var manager = new PropertyManager { User = managerUser };

        context.PropertyManagers.Add(manager);
        context.PropertyManagers.Add(new PropertyManager { User = otherManagerUser });

        var activeType = new UnitType
        {
            Name = $"Active Type {suffix}",
            IsActive = true
        };

        var inactiveType = new UnitType
        {
            Name = $"Inactive Type {suffix}",
            IsActive = false
        };

        context.UnitTypes.AddRange(activeType, inactiveType);

        var property = new Property
        {
            Name = $"Property {suffix}",
            Address = "1 Test Street",
            PropertyManager = manager
        };

        var unit = new Unit
        {
            Property = property,
            UnitType = activeType,
            UnitNumber = "101",
            Bedrooms = 2,
            MonthlyRent = 1500m
        };

        context.Properties.Add(property);
        context.Units.Add(unit);

        await context.SaveChangesAsync();

        return new ManagedPropertyScenario(
            managerUser,
            otherManagerUser,
            property,
            unit,
            activeType,
            inactiveType);
    }

    public static async Task<(ApplicationDbContext Context, ManagedPropertyScenario Scenario)>
        CreateWithContextAsync()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        var context = database.CreateContext();
        var scenario = await CreateAsync(context);

        return (context, scenario);
    }

    // An expired lease: enough history to force archiving instead of deletion.
    public static async Task AddLeaseAsync(
        ApplicationDbContext context,
        Unit unit)
    {
        var suffix = Guid.NewGuid().ToString("N");

        context.Leases.Add(new Lease
        {
            UnitId = unit.Id,
            Applicant = new Applicant
            {
                User = NewUser($"tenant-{suffix}"),
                FirstName = "Test",
                LastName = "Tenant",
                PhoneNumber = "555-0100",
                CurrentAddress = "5 Tenant Street"
            },
            StartDate = DateTime.UtcNow.Date.AddYears(-2),
            EndDate = DateTime.UtcNow.Date.AddYears(-1),
            AgreedRent = unit.MonthlyRent
        });

        await context.SaveChangesAsync();
    }

    private static ApplicationUser NewUser(string name) =>
        new()
        {
            UserName = $"{name}@test.local",
            Email = $"{name}@test.local"
        };
}
