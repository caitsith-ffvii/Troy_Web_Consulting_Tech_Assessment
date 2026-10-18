using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Web.Services;

public class PropertyService(ApplicationDbContext context)
{
    // Every property manager can view every non-archived property.
    public async Task<List<Property>> GetActivePropertiesAsync()
    {
        return await context.Properties
            .AsNoTracking()
            .Include(p => p.PropertyManager)
                .ThenInclude(m => m.User)
            .Include(p => p.Units.Where(u => !u.IsArchived))
                .ThenInclude(u => u.UnitType)
            .Where(p => !p.IsArchived)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<Property?> FindManagedPropertyAsync(
        int propertyId,
        string managerUserId)
    {
        return await context.Properties
            .AsNoTracking()
            .SingleOrDefaultAsync(p =>
                p.Id == propertyId &&
                !p.IsArchived &&
                p.PropertyManager.UserId == managerUserId);
    }

    public async Task<PropertyResult> CreateAsync(
        string managerUserId,
        string name,
        string address)
    {
        var managerId = await context.PropertyManagers
            .Where(m => m.UserId == managerUserId)
            .Select(m => (int?)m.Id)
            .SingleOrDefaultAsync();

        if (managerId is null)
        {
            return PropertyResult.Failure(
                null,
                "Only property managers can create properties.");
        }

        var errors = Validate(name, address);

        if (errors.Count > 0)
        {
            return PropertyResult.Failure(errors);
        }

        // The creating manager is assigned to the new property.
        var property = new Property
        {
            Name = name.Trim(),
            Address = address.Trim(),
            PropertyManagerId = managerId.Value
        };

        context.Properties.Add(property);
        await context.SaveChangesAsync();

        return PropertyResult.Success(property);
    }

    public async Task<PropertyResult> UpdateAsync(
        int propertyId,
        string managerUserId,
        string name,
        string address)
    {
        var property = await FindEditablePropertyAsync(
            propertyId,
            managerUserId);

        if (property is null)
        {
            return PropertyResult.Failure(
                null,
                "Property not found, or you are not its assigned manager.");
        }

        var errors = Validate(name, address);

        if (errors.Count > 0)
        {
            return PropertyResult.Failure(errors);
        }

        property.Name = name.Trim();
        property.Address = address.Trim();

        await context.SaveChangesAsync();

        return PropertyResult.Success(property);
    }

    // Deletes the property and its units when nothing references them;
    // otherwise archives the property so its history is preserved.
    public async Task<RemoveResult> RemoveAsync(
        int propertyId,
        string managerUserId)
    {
        var property = await FindEditablePropertyAsync(
            propertyId,
            managerUserId);

        if (property is null)
        {
            return RemoveResult.Failure(
                "Property not found, or you are not its assigned manager.");
        }

        if (await HasHistoryAsync(propertyId))
        {
            // Archiving a property does not archive its units.
            property.IsArchived = true;
            await context.SaveChangesAsync();

            return RemoveResult.Success(RemovalOutcome.Archived);
        }

        var units = await context.Units
            .Where(u => u.PropertyId == propertyId)
            .ToListAsync();

        context.Units.RemoveRange(units);
        context.Properties.Remove(property);
        await context.SaveChangesAsync();

        return RemoveResult.Success(RemovalOutcome.Deleted);
    }

    // True when removing the property would archive rather than delete it.
    public async Task<bool> HasHistoryAsync(int propertyId)
    {
        return
            await context.RentalApplications
                .AnyAsync(a => a.Unit.PropertyId == propertyId) ||
            await context.Leases
                .AnyAsync(l => l.Unit.PropertyId == propertyId);
    }

    private async Task<Property?> FindEditablePropertyAsync(
        int propertyId,
        string managerUserId)
    {
        return await context.Properties
            .SingleOrDefaultAsync(p =>
                p.Id == propertyId &&
                !p.IsArchived &&
                p.PropertyManager.UserId == managerUserId);
    }

    private static List<ServiceError> Validate(string name, string address)
    {
        var errors = new List<ServiceError>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new ServiceError("Name", "Name is required."));
        }
        else if (name.Trim().Length > 150)
        {
            errors.Add(new ServiceError(
                "Name",
                "Name cannot exceed 150 characters."));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            errors.Add(new ServiceError("Address", "Address is required."));
        }
        else if (address.Trim().Length > 300)
        {
            errors.Add(new ServiceError(
                "Address",
                "Address cannot exceed 300 characters."));
        }

        return errors;
    }
}

public sealed record ServiceError(
    string? Field,
    string Message);

public sealed record PropertyResult(
    bool Succeeded,
    Property? Property,
    IReadOnlyList<ServiceError> Errors)
{
    public static PropertyResult Success(Property property) =>
        new(true, property, []);

    public static PropertyResult Failure(
        IReadOnlyList<ServiceError> errors) =>
        new(false, null, errors);

    public static PropertyResult Failure(string? field, string message) =>
        new(false, null, [new ServiceError(field, message)]);
}

public enum RemovalOutcome
{
    Deleted,
    Archived
}

public sealed record RemoveResult(
    bool Succeeded,
    RemovalOutcome? Outcome,
    string? Error)
{
    public static RemoveResult Success(RemovalOutcome outcome) =>
        new(true, outcome, null);

    public static RemoveResult Failure(string error) =>
        new(false, null, error);
}
