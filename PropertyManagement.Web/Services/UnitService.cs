using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Web.Services;

public class UnitService(ApplicationDbContext context)
{
    // Active types, plus the unit's current type when it has since been
    // deactivated, so an existing unit can keep it.
    public async Task<List<UnitType>> GetSelectableUnitTypesAsync(
        int? currentUnitTypeId)
    {
        return await context.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive || t.Id == currentUnitTypeId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    // True when removing the unit would archive rather than delete it.
    public async Task<bool> HasHistoryAsync(int unitId)
    {
        return
            await context.RentalApplications.AnyAsync(a => a.UnitId == unitId) ||
            await context.Leases.AnyAsync(l => l.UnitId == unitId);
    }

    public async Task<Unit?> FindManagedUnitAsync(
        int unitId,
        string managerUserId)
    {
        return await context.Units
            .AsNoTracking()
            .Include(u => u.UnitType)
            .SingleOrDefaultAsync(u =>
                u.Id == unitId &&
                !u.IsArchived &&
                u.Property.PropertyManager.UserId == managerUserId);
    }

    public async Task<UnitResult> CreateAsync(
        int propertyId,
        string managerUserId,
        UnitDetails details)
    {
        var propertyIsEditable = await context.Properties
            .AnyAsync(p =>
                p.Id == propertyId &&
                !p.IsArchived &&
                p.PropertyManager.UserId == managerUserId);

        if (!propertyIsEditable)
        {
            return UnitResult.Failure(
                null,
                "Property not found, or you are not its assigned manager.");
        }

        var errors = await ValidateAsync(
            propertyId,
            unitId: null,
            currentUnitTypeId: null,
            details);

        if (errors.Count > 0)
        {
            return UnitResult.Failure(errors);
        }

        var unit = new Unit
        {
            PropertyId = propertyId,
            UnitNumber = details.UnitNumber.Trim(),
            Bedrooms = details.Bedrooms,
            MonthlyRent = details.MonthlyRent,
            UnitTypeId = details.UnitTypeId
        };

        context.Units.Add(unit);
        await context.SaveChangesAsync();

        return UnitResult.Success(unit);
    }

    public async Task<UnitResult> UpdateAsync(
        int unitId,
        string managerUserId,
        UnitDetails details)
    {
        var unit = await FindEditableUnitAsync(unitId, managerUserId);

        if (unit is null)
        {
            return UnitResult.Failure(
                null,
                "Unit not found, or you are not its property's assigned manager.");
        }

        var errors = await ValidateAsync(
            unit.PropertyId,
            unit.Id,
            unit.UnitTypeId,
            details);

        if (errors.Count > 0)
        {
            return UnitResult.Failure(errors);
        }

        unit.UnitNumber = details.UnitNumber.Trim();
        unit.Bedrooms = details.Bedrooms;
        unit.MonthlyRent = details.MonthlyRent;
        unit.UnitTypeId = details.UnitTypeId;

        await context.SaveChangesAsync();

        return UnitResult.Success(unit);
    }

    // Deletes a unit nothing references; otherwise archives it so its
    // applications and leases are preserved.
    public async Task<RemoveResult> RemoveAsync(
        int unitId,
        string managerUserId)
    {
        var unit = await FindEditableUnitAsync(unitId, managerUserId);

        if (unit is null)
        {
            return RemoveResult.Failure(
                "Unit not found, or you are not its property's assigned manager.");
        }

        if (await HasHistoryAsync(unitId))
        {
            unit.IsArchived = true;
            await context.SaveChangesAsync();

            return RemoveResult.Success(RemovalOutcome.Archived);
        }

        context.Units.Remove(unit);
        await context.SaveChangesAsync();

        return RemoveResult.Success(RemovalOutcome.Deleted);
    }

    private async Task<Unit?> FindEditableUnitAsync(
        int unitId,
        string managerUserId)
    {
        return await context.Units
            .SingleOrDefaultAsync(u =>
                u.Id == unitId &&
                !u.IsArchived &&
                !u.Property.IsArchived &&
                u.Property.PropertyManager.UserId == managerUserId);
    }

    private async Task<List<ServiceError>> ValidateAsync(
        int propertyId,
        int? unitId,
        int? currentUnitTypeId,
        UnitDetails details)
    {
        var errors = new List<ServiceError>();

        if (string.IsNullOrWhiteSpace(details.UnitNumber))
        {
            errors.Add(new ServiceError(
                "UnitNumber",
                "Unit number is required."));
        }
        else if (details.UnitNumber.Trim().Length > 50)
        {
            errors.Add(new ServiceError(
                "UnitNumber",
                "Unit number cannot exceed 50 characters."));
        }
        else
        {
            // Archived units keep their numbers, so they count as taken.
            var unitNumber = details.UnitNumber.Trim();

            var numberTaken = await context.Units.AnyAsync(u =>
                u.PropertyId == propertyId &&
                u.UnitNumber == unitNumber &&
                u.Id != unitId);

            if (numberTaken)
            {
                errors.Add(new ServiceError(
                    "UnitNumber",
                    "This property already has a unit with that number."));
            }
        }

        if (details.Bedrooms is < 0 or > 20)
        {
            errors.Add(new ServiceError(
                "Bedrooms",
                "Bedrooms must be between 0 and 20."));
        }

        if (details.MonthlyRent <= 0)
        {
            errors.Add(new ServiceError(
                "MonthlyRent",
                "Monthly rent must be greater than zero."));
        }

        // An inactive type may stay on a unit that already has it, but
        // cannot be newly selected.
        var keepsCurrentType = details.UnitTypeId == currentUnitTypeId;

        var typeIsSelectable = keepsCurrentType ||
            await context.UnitTypes.AnyAsync(t =>
                t.Id == details.UnitTypeId &&
                t.IsActive);

        if (!typeIsSelectable)
        {
            errors.Add(new ServiceError(
                "UnitTypeId",
                "Select an active unit type."));
        }

        return errors;
    }
}

public sealed record UnitDetails(
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    int UnitTypeId);

public sealed record UnitResult(
    bool Succeeded,
    Unit? Unit,
    IReadOnlyList<ServiceError> Errors)
{
    public static UnitResult Success(Unit unit) =>
        new(true, unit, []);

    public static UnitResult Failure(
        IReadOnlyList<ServiceError> errors) =>
        new(false, null, errors);

    public static UnitResult Failure(string? field, string message) =>
        new(false, null, [new ServiceError(field, message)]);
}
