using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.Properties;

namespace PropertyManagement.Web.Controllers;

// Modal actions return the form partial when it needs re-rendering and
// 204 No Content on success, which tells the modal script to close and
// refresh the property list.
[Authorize(Roles = Roles.PropertyManager)]
public class PropertiesController(PropertyService propertyService)
    : Controller
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index() =>
        View(await BuildListAsync());

    [HttpGet]
    public async Task<IActionResult> List() =>
        PartialView("_PropertyList", await BuildListAsync());

    [HttpGet]
    public IActionResult Create() =>
        PartialView("_PropertyForm", new PropertyFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_PropertyForm", model);
        }

        var result = await propertyService.CreateAsync(
            CurrentUserId,
            model.Name,
            model.Address);

        return HandleFormResult(result.Succeeded, result.Errors, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var property = await propertyService.FindManagedPropertyAsync(
            id,
            CurrentUserId);

        if (property is null)
        {
            return NotFound();
        }

        return PartialView("_PropertyForm", new PropertyFormViewModel
        {
            Id = property.Id,
            Name = property.Name,
            Address = property.Address
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return PartialView("_PropertyForm", model);
        }

        var result = await propertyService.UpdateAsync(
            id,
            CurrentUserId,
            model.Name,
            model.Address);

        return HandleFormResult(result.Succeeded, result.Errors, model);
    }

    [HttpGet]
    public async Task<IActionResult> Remove(int id)
    {
        var property = await propertyService.FindManagedPropertyAsync(
            id,
            CurrentUserId);

        if (property is null)
        {
            return NotFound();
        }

        var willArchive = await propertyService.HasHistoryAsync(id);

        return PartialView("_RemoveConfirm", new RemoveConfirmViewModel
        {
            Title = $"Remove {property.Name}",
            Message = willArchive
                ? "This property has applications or leases, so it will be archived: hidden from lists and closed to new applications, with its history kept."
                : "This property and its units have no applications or leases, so they will be permanently deleted.",
            ActionUrl = Url.Action(nameof(Remove), new { id })!
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Remove))]
    public async Task<IActionResult> RemoveConfirmed(int id)
    {
        var result = await propertyService.RemoveAsync(id, CurrentUserId);

        if (!result.Succeeded)
        {
            return NotFound();
        }

        return NoContent();
    }

    private IActionResult HandleFormResult(
        bool succeeded,
        IReadOnlyList<ServiceError> errors,
        PropertyFormViewModel model)
    {
        if (succeeded)
        {
            return NoContent();
        }

        foreach (var error in errors)
        {
            ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
        }

        return PartialView("_PropertyForm", model);
    }

    private async Task<PropertyListViewModel> BuildListAsync()
    {
        var properties = await propertyService.GetActivePropertiesAsync();

        return new PropertyListViewModel
        {
            Properties = properties
                .Select(p => new PropertyRowViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    ManagerEmail = p.PropertyManager.User.Email ?? string.Empty,
                    CanEdit = p.PropertyManager.UserId == CurrentUserId,
                    Units = p.Units
                        .OrderBy(u => u.UnitNumber)
                        .Select(u => new UnitRowViewModel
                        {
                            Id = u.Id,
                            UnitNumber = u.UnitNumber,
                            Bedrooms = u.Bedrooms,
                            MonthlyRent = u.MonthlyRent,
                            UnitTypeName = u.UnitType.Name,
                            UnitTypeIsActive = u.UnitType.IsActive
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
