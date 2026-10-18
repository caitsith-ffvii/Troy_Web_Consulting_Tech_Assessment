using Bogus;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Data;

// Seeds demo users and sample data for local review. Users are checked one
// by one; sample data is inserted once, all-or-nothing, and never updated.
public class DemoDataSeeder(
    ApplicationDbContext context,
    RegistrationService registrationService)
{
    public const string DemoPassword = "Demo123!";

    public static readonly string[] ManagerEmails =
    [
        "manager1@demo.local",
        "manager2@demo.local"
    ];

    public static readonly string[] ApplicantEmails =
    [
        "applicant1@demo.local",
        "applicant2@demo.local",
        "applicant3@demo.local",
        "applicant4@demo.local",
        "applicant5@demo.local"
    ];

    // The first property doubles as the marker that sample data exists.
    public static readonly string[] PropertyNames =
    [
        "Maple Court",
        "Riverside Commons",
        "Oak Hollow",
        "Harbor View"
    ];

    private static readonly string[] ActiveUnitTypes =
    [
        "Apartment",
        "Townhouse",
        "Studio"
    ];

    private const string InactiveUnitType = "Loft";

    private const int UnitsPerProperty = 4;

    // Fixed seed so the generated data is the same on every machine.
    private readonly Faker faker = new("en")
    {
        Random = new Randomizer(20261018)
    };

    public async Task SeedAsync()
    {
        await SeedUnitTypesAsync();
        await SeedUsersAsync();

        if (await context.Properties.AnyAsync(p => p.Name == PropertyNames[0]))
        {
            return;
        }

        await SeedSampleDataAsync();
    }

    private async Task SeedUnitTypesAsync()
    {
        foreach (var name in ActiveUnitTypes.Append(InactiveUnitType))
        {
            if (await context.UnitTypes.AnyAsync(t => t.Name == name))
            {
                continue;
            }

            context.UnitTypes.Add(new UnitType
            {
                Name = name,
                IsActive = name != InactiveUnitType
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        foreach (var email in ManagerEmails)
        {
            await EnsureUserAsync(new RegistrationRequest(
                email,
                DemoPassword,
                Roles.PropertyManager,
                null,
                null,
                null,
                null));
        }

        foreach (var email in ApplicantEmails)
        {
            await EnsureUserAsync(new RegistrationRequest(
                email,
                DemoPassword,
                Roles.Applicant,
                faker.Name.FirstName(),
                faker.Name.LastName(),
                faker.Phone.PhoneNumber("(###) ###-####"),
                faker.Address.FullAddress()));
        }
    }

    // Existing accounts are left untouched, including their passwords.
    private async Task EnsureUserAsync(RegistrationRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return;
        }

        var result = await registrationService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed demo user '{request.Email}'.");
        }
    }

    private async Task SeedSampleDataAsync()
    {
        var managers = await context.PropertyManagers
            .Where(m => ManagerEmails.Contains(m.User.Email!))
            .OrderBy(m => m.User.Email)
            .ToListAsync();

        var applicants = await context.Applicants
            .Include(a => a.User)
            .Where(a => ApplicantEmails.Contains(a.User.Email!))
            .OrderBy(a => a.User.Email)
            .ToListAsync();

        if (managers.Count != ManagerEmails.Length ||
            applicants.Count != ApplicantEmails.Length)
        {
            throw new InvalidOperationException(
                "Demo users must have profiles before sample data is seeded.");
        }

        var unitTypes = await context.UnitTypes
            .Where(t => ActiveUnitTypes.Contains(t.Name) || t.Name == InactiveUnitType)
            .ToDictionaryAsync(t => t.Name);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var units = CreatePropertiesAndUnits(managers, unitTypes);
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Three units get active leases from approved applications; the rest
        // stay available for open applications.
        var leasedUnits = new[] { units[0], units[4], units[8] };
        var openUnits = units.Except(leasedUnits).ToList();

        for (var i = 0; i < leasedUnits.Length; i++)
        {
            var applicant = applicants[i % applicants.Count];
            var unit = leasedUnits[i];

            var approved = CreateApplication(
                applicant,
                unit,
                RentalApplicationStatus.Approved,
                submittedAtUtc: now.AddDays(-60 - i));

            context.Leases.Add(new Lease
            {
                Unit = unit,
                ApplicantId = applicant.Id,
                RentalApplication = approved,
                StartDate = today.AddDays(-30 - i),
                EndDate = today.AddDays(-30 - i).AddMonths(12).AddDays(-1),
                AgreedRent = unit.MonthlyRent
            });
        }

        // A past lease that no longer covers today, so the unit is available.
        context.Leases.Add(new Lease
        {
            Unit = openUnits[0],
            ApplicantId = applicants[0].Id,
            StartDate = today.AddYears(-2),
            EndDate = today.AddYears(-1),
            AgreedRent = openUnits[0].MonthlyRent
        });

        // Over 20 submitted applications so the default review queue pages.
        var statuses =
            Enumerable.Repeat(RentalApplicationStatus.Submitted, 22)
                .Concat(Enumerable.Repeat(RentalApplicationStatus.Draft, 3))
                .Concat(Enumerable.Repeat(RentalApplicationStatus.Returned, 3))
                .Concat(Enumerable.Repeat(RentalApplicationStatus.Denied, 3))
                .Concat(Enumerable.Repeat(RentalApplicationStatus.Withdrawn, 2))
                .ToList();

        for (var i = 0; i < statuses.Count; i++)
        {
            // Unit and applicant cycle independently, so no applicant applies
            // to the same unit twice.
            CreateApplication(
                applicants[i % applicants.Count],
                openUnits[i % openUnits.Count],
                statuses[i],
                submittedAtUtc: now.AddDays(-(i + 1)));
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private List<Unit> CreatePropertiesAndUnits(
        List<PropertyManager> managers,
        Dictionary<string, UnitType> unitTypes)
    {
        var units = new List<Unit>();

        for (var p = 0; p < PropertyNames.Length; p++)
        {
            var property = new Property
            {
                Name = PropertyNames[p],
                Address = faker.Address.FullAddress(),
                // Two properties per manager.
                PropertyManager = managers[p / 2]
            };

            context.Properties.Add(property);

            for (var u = 0; u < UnitsPerProperty; u++)
            {
                var index = p * UnitsPerProperty + u;
                var isLast = index == PropertyNames.Length * UnitsPerProperty - 1;

                // The last unit keeps an inactive type to show it still displays.
                var unitType = isLast
                    ? unitTypes[InactiveUnitType]
                    : unitTypes[ActiveUnitTypes[index % ActiveUnitTypes.Length]];

                var unit = new Unit
                {
                    Property = property,
                    UnitType = unitType,
                    UnitNumber = $"{u / 2 + 1}0{u % 2 + 1}",
                    Bedrooms = unitType.Name == "Studio"
                        ? 0
                        : faker.Random.Int(1, 3),
                    MonthlyRent = faker.Random.Int(36, 100) * 25m
                };

                context.Units.Add(unit);
                units.Add(unit);
            }
        }

        return units;
    }

    private RentalApplication CreateApplication(
        Applicant applicant,
        Unit unit,
        RentalApplicationStatus status,
        DateTime submittedAtUtc)
    {
        var isDraft = status == RentalApplicationStatus.Draft;
        var createdAtUtc = submittedAtUtc.AddDays(-2);
        var reviewedAtUtc = submittedAtUtc.AddHours(20);

        var application = new RentalApplication
        {
            Applicant = applicant,
            Unit = unit,
            Status = status,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = isDraft ? createdAtUtc : reviewedAtUtc,
            SubmittedAtUtc = isDraft ? null : submittedAtUtc,
            ApplicantInformationCompleted = !isDraft,
            ResidenceHistoryCompleted = !isDraft,
            ApplicantInformation = new ApplicantInformationSnapshot
            {
                FirstName = applicant.FirstName,
                LastName = applicant.LastName,
                PhoneNumber = applicant.PhoneNumber,
                Email = applicant.User.Email!,
                CurrentAddress = applicant.CurrentAddress
            }
        };

        if (!isDraft)
        {
            AddResidenceHistory(application);
        }

        context.RentalApplications.Add(application);

        if (isDraft)
        {
            return application;
        }

        AddHistory(
            application,
            applicant.UserId,
            submittedAtUtc,
            RentalApplicationStatus.Draft,
            RentalApplicationStatus.Submitted,
            comment: null);

        if (status == RentalApplicationStatus.Submitted)
        {
            return application;
        }

        // Withdrawal is the applicant's action; review outcomes are the
        // property's assigned manager's.
        var changedByUserId = status == RentalApplicationStatus.Withdrawn
            ? applicant.UserId
            : unit.Property.PropertyManager.UserId;

        var comment = status switch
        {
            RentalApplicationStatus.Returned =>
                "Please add your previous residences from the last three years.",
            RentalApplicationStatus.Denied =>
                "Income verification did not meet the requirements for this unit.",
            _ => null
        };

        AddHistory(
            application,
            changedByUserId,
            reviewedAtUtc,
            RentalApplicationStatus.Submitted,
            status,
            comment);

        return application;
    }

    private void AddResidenceHistory(RentalApplication application)
    {
        var entryCount = faker.Random.Int(0, 2);
        var moveOut = DateTime.UtcNow.Date.AddMonths(-faker.Random.Int(1, 6));

        for (var i = 0; i < entryCount; i++)
        {
            var moveIn = moveOut.AddYears(-faker.Random.Int(1, 3));

            application.ResidenceHistory.Add(new ResidenceHistoryEntry
            {
                Address = faker.Address.FullAddress(),
                LandlordName = faker.Name.FullName(),
                LandlordPhoneNumber = faker.Phone.PhoneNumber("(###) ###-####"),
                MoveInDate = moveIn,
                MoveOutDate = moveOut
            });

            moveOut = moveIn;
        }
    }

    private static void AddHistory(
        RentalApplication application,
        string changedByUserId,
        DateTime changedAtUtc,
        RentalApplicationStatus previousStatus,
        RentalApplicationStatus newStatus,
        string? comment)
    {
        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = changedAtUtc,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Comment = comment
        });
    }
}
