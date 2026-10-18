using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Data;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Services;

public class PropertyServiceTests
{
    [Fact]
    public async Task CreateAsync_PropertyManager_CreatesPropertyAssignedToCreator()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new PropertyService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.OtherManagerUser.Id,
            "  New Property  ",
            "2 New Street");

        // Assert
        Assert.True(result.Succeeded);

        var property = await context.Properties
            .Include(p => p.PropertyManager)
            .SingleAsync(p => p.Id == result.Property!.Id);

        Assert.Equal("New Property", property.Name);
        Assert.Equal(scenario.OtherManagerUser.Id, property.PropertyManager.UserId);
    }

    [Fact]
    public async Task CreateAsync_UserIsNotPropertyManager_Rejects()
    {
        // Arrange
        var (context, _) = await PropertyTestData.CreateWithContextAsync();
        var service = new PropertyService(context);
        var name = $"Rejected {Guid.NewGuid():N}";

        // Act
        var result = await service.CreateAsync(
            "not-a-manager",
            name,
            "2 New Street");

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(await context.Properties.AnyAsync(p => p.Name == name));
    }

    [Fact]
    public async Task CreateAsync_BlankFields_ReturnsFieldErrors()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new PropertyService(context);

        // Act
        var result = await service.CreateAsync(
            scenario.ManagerUser.Id,
            " ",
            "");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "Name");
        Assert.Contains(result.Errors, e => e.Field == "Address");
    }

    [Fact]
    public async Task UpdateAsync_AssignedManager_UpdatesProperty()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new PropertyService(context);

        // Act
        var result = await service.UpdateAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id,
            "Renamed",
            "3 Renamed Street");

        // Assert
        Assert.True(result.Succeeded);

        context.ChangeTracker.Clear();

        var property = await context.Properties
            .SingleAsync(p => p.Id == scenario.Property.Id);

        Assert.Equal("Renamed", property.Name);
        Assert.Equal("3 Renamed Street", property.Address);
    }

    [Fact]
    public async Task UpdateAsync_UnassignedManager_RejectsWithoutChanges()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new PropertyService(context);

        // Act
        var result = await service.UpdateAsync(
            scenario.Property.Id,
            scenario.OtherManagerUser.Id,
            "Renamed",
            "3 Renamed Street");

        // Assert
        Assert.False(result.Succeeded);

        context.ChangeTracker.Clear();

        var property = await context.Properties
            .SingleAsync(p => p.Id == scenario.Property.Id);

        Assert.Equal(scenario.Property.Name, property.Name);
    }

    [Fact]
    public async Task RemoveAsync_NoApplicationsOrLeases_DeletesPropertyAndUnits()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new PropertyService(context);

        // Act
        var result = await service.RemoveAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(RemovalOutcome.Deleted, result.Outcome);
        Assert.False(await context.Properties
            .AnyAsync(p => p.Id == scenario.Property.Id));
        Assert.False(await context.Units
            .AnyAsync(u => u.Id == scenario.Unit.Id));
    }

    [Fact]
    public async Task RemoveAsync_UnitHasLease_ArchivesPropertyButNotUnits()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        await PropertyTestData.AddLeaseAsync(context, scenario.Unit);

        var service = new PropertyService(context);

        // Act
        var result = await service.RemoveAsync(
            scenario.Property.Id,
            scenario.ManagerUser.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(RemovalOutcome.Archived, result.Outcome);

        context.ChangeTracker.Clear();

        var property = await context.Properties
            .SingleAsync(p => p.Id == scenario.Property.Id);
        var unit = await context.Units
            .SingleAsync(u => u.Id == scenario.Unit.Id);

        Assert.True(property.IsArchived);
        Assert.False(unit.IsArchived);
    }

    [Fact]
    public async Task RemoveAsync_UnassignedManager_RejectsWithoutChanges()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();
        var service = new PropertyService(context);

        // Act
        var result = await service.RemoveAsync(
            scenario.Property.Id,
            scenario.OtherManagerUser.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(await context.Properties
            .AnyAsync(p => p.Id == scenario.Property.Id && !p.IsArchived));
    }

    [Fact]
    public async Task GetActivePropertiesAsync_IncludesOtherManagersPropertiesAndExcludesArchived()
    {
        // Arrange
        var (context, scenario) = await PropertyTestData.CreateWithContextAsync();

        var archived = new Property
        {
            Name = $"Archived {Guid.NewGuid():N}",
            Address = "4 Archived Street",
            PropertyManagerId = scenario.Property.PropertyManagerId,
            IsArchived = true
        };

        context.Properties.Add(archived);
        await context.SaveChangesAsync();

        var service = new PropertyService(context);

        // Act
        var properties = await service.GetActivePropertiesAsync();

        // Assert
        Assert.Contains(properties, p => p.Id == scenario.Property.Id);
        Assert.DoesNotContain(properties, p => p.Id == archived.Id);
    }

}
