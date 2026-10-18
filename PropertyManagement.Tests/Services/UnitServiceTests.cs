using Microsoft.EntityFrameworkCore;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Services;

public class UnitServiceTests
{
    [Fact]
    public async Task CreateAsync_ActiveUnitType_CreatesUnit()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id,
            new UnitDetails("102", 1, 1200m, scenario.ActiveUnitType.Id));

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(await context.Units.AnyAsync(u =>
            u.PropertyId == scenario.Property.Id &&
            u.UnitNumber == "102"));
    }

    [Fact]
    public async Task CreateAsync_InactiveUnitType_Rejects()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id,
            new UnitDetails("102", 1, 1200m, scenario.InactiveUnitType.Id));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "UnitTypeId");
    }

    [Fact]
    public async Task CreateAsync_DuplicateUnitNumber_Rejects()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id,
            new UnitDetails(
                scenario.Unit.UnitNumber,
                1,
                1200m,
                scenario.ActiveUnitType.Id));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "UnitNumber");
    }

    [Fact]
    public async Task CreateAsync_InvalidValues_ReturnsFieldErrors()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id,
            new UnitDetails(" ", -1, 0m, scenario.ActiveUnitType.Id));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "UnitNumber");
        Assert.Contains(result.Errors, e => e.Field == "Bedrooms");
        Assert.Contains(result.Errors, e => e.Field == "MonthlyRent");
    }

    [Fact]
    public async Task CreateAsync_UnassignedManager_Rejects()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.Property.Id,
            scenario.OtherManagerUser.Id,
            new UnitDetails("102", 1, 1200m, scenario.ActiveUnitType.Id));

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(await context.Units.AnyAsync(u =>
            u.PropertyId == scenario.Property.Id &&
            u.UnitNumber == "102"));
    }

    [Fact]
    public async Task CreateAsync_ArchivedProperty_Rejects()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();

        scenario.Property.IsArchived = true;
        await context.SaveChangesAsync();

        var service = new UnitService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id,
            new UnitDetails("102", 1, 1200m, scenario.ActiveUnitType.Id));

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task UpdateAsync_UnitKeepsInactiveType_UpdatesOtherFields()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();

        scenario.Unit.UnitTypeId = scenario.InactiveUnitType.Id;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new UnitService(context);

        // Act
        var result = await service.UpdateAsync(
            scenario.Unit.Id,
            scenario.ManagerUser.Id,
            new UnitDetails("101", 3, 1750m, scenario.InactiveUnitType.Id));

        // Assert
        Assert.True(result.Succeeded);

        context.ChangeTracker.Clear();

        var unit = await context.Units.SingleAsync(u => u.Id == scenario.Unit.Id);

        Assert.Equal(3, unit.Bedrooms);
        Assert.Equal(1750m, unit.MonthlyRent);
        Assert.Equal(scenario.InactiveUnitType.Id, unit.UnitTypeId);
    }

    [Fact]
    public async Task UpdateAsync_ChangesToInactiveType_Rejects()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.UpdateAsync(
            scenario.Unit.Id,
            scenario.ManagerUser.Id,
            new UnitDetails("101", 2, 1500m, scenario.InactiveUnitType.Id));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "UnitTypeId");

        context.ChangeTracker.Clear();

        var unit = await context.Units.SingleAsync(u => u.Id == scenario.Unit.Id);

        Assert.Equal(scenario.ActiveUnitType.Id, unit.UnitTypeId);
    }

    [Fact]
    public async Task GetSelectableUnitTypesAsync_IncludesCurrentInactiveTypeOnly()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();

        var otherInactiveType = new Domain.Entities.UnitType
        {
            Name = $"Other Inactive {Guid.NewGuid():N}",
            IsActive = false
        };

        context.UnitTypes.Add(otherInactiveType);
        await context.SaveChangesAsync();

        var service = new UnitService(context);

        // Act
        var withCurrent = await service.GetSelectableUnitTypesAsync(
            scenario.InactiveUnitType.Id);
        var withoutCurrent = await service.GetSelectableUnitTypesAsync(null);

        // Assert
        Assert.Contains(withCurrent, t => t.Id == scenario.InactiveUnitType.Id);
        Assert.Contains(withCurrent, t => t.Id == scenario.ActiveUnitType.Id);
        Assert.DoesNotContain(withCurrent, t => t.Id == otherInactiveType.Id);
        Assert.DoesNotContain(withoutCurrent, t => !t.IsActive);
    }

    [Fact]
    public async Task RemoveAsync_NoApplicationsOrLeases_DeletesUnit()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.RemoveAsync(
            scenario.Unit.Id,
            scenario.ManagerUser.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(RemovalOutcome.Deleted, result.Outcome);
        Assert.False(await context.Units.AnyAsync(u => u.Id == scenario.Unit.Id));
    }

    [Fact]
    public async Task RemoveAsync_UnitHasLease_ArchivesUnit()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        await PropertyTestData.AddLeaseAsync(context, scenario.Unit);

        var service = new UnitService(context);

        // Act
        var result = await service.RemoveAsync(
            scenario.Unit.Id,
            scenario.ManagerUser.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(RemovalOutcome.Archived, result.Outcome);

        context.ChangeTracker.Clear();

        var unit = await context.Units.SingleAsync(u => u.Id == scenario.Unit.Id);

        Assert.True(unit.IsArchived);
    }

    [Fact]
    public async Task RemoveAsync_UnassignedManager_RejectsWithoutChanges()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new UnitService(context);

        // Act
        var result = await service.RemoveAsync(
            scenario.Unit.Id,
            scenario.OtherManagerUser.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(await context.Units.AnyAsync(u =>
            u.Id == scenario.Unit.Id &&
            !u.IsArchived));
    }
}
