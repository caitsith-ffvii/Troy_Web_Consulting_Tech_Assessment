using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.Properties;

namespace PropertyManagement.Web.Controllers;

// Same modal conventions as PropertiesController: partial on re-render,
// 204 No Content on success.
[Authorize(Roles = Roles.PropertyManager)]
public class UnitsController(
    UnitService unitService,
    PropertyService propertyService)
    : Controller
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId)
    {
        var property = await propertyService.FindManagedPropertyAsync(
            propertyId,
            CurrentUserId);

        if (property is null)
        {
            return NotFound();
        }

        return PartialView("_UnitForm", new UnitFormViewModel
        {
            PropertyId = propertyId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitFormViewModel model)
    {
        model.Id = null;
        model.CurrentUnitTypeId = null;

        if (!ModelState.IsValid)
        {
            return PartialView("_UnitForm", model);
        }

        var result = await unitService.CreateAsync(
            model.PropertyId,
            CurrentUserId,
            ToDetails(model));

        return HandleFormResult(result.Succeeded, result.Errors, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var unit = await unitService.FindManagedUnitAsync(id, CurrentUserId);

        if (unit is null)
        {
            return NotFound();
        }

        return PartialView("_UnitForm", new UnitFormViewModel
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            UnitNumber = unit.UnitNumber,
            Bedrooms = unit.Bedrooms,
            MonthlyRent = unit.MonthlyRent,
            UnitTypeId = unit.UnitTypeId,
            CurrentUnitTypeId = unit.UnitTypeId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model)
    {
        var unit = await unitService.FindManagedUnitAsync(id, CurrentUserId);

        if (unit is null)
        {
            return NotFound();
        }

        // Re-renders use the saved type, not a posted value.
        model.Id = id;
        model.PropertyId = unit.PropertyId;
        model.CurrentUnitTypeId = unit.UnitTypeId;

        if (!ModelState.IsValid)
        {
            return PartialView("_UnitForm", model);
        }

        var result = await unitService.UpdateAsync(
            id,
            CurrentUserId,
            ToDetails(model));

        return HandleFormResult(result.Succeeded, result.Errors, model);
    }

    [HttpGet]
    public async Task<IActionResult> Remove(int id)
    {
        var unit = await unitService.FindManagedUnitAsync(id, CurrentUserId);

        if (unit is null)
        {
            return NotFound();
        }

        var willArchive = await unitService.HasHistoryAsync(id);

        return PartialView("_RemoveConfirm", new RemoveConfirmViewModel
        {
            Title = $"Remove unit {unit.UnitNumber}",
            Message = willArchive
                ? "This unit has applications or leases, so it will be archived: hidden from lists and closed to new applications, with its history kept."
                : "This unit has no applications or leases, so it will be permanently deleted.",
            ActionUrl = Url.Action(nameof(Remove), new { id })!
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Remove))]
    public async Task<IActionResult> RemoveConfirmed(int id)
    {
        var result = await unitService.RemoveAsync(id, CurrentUserId);

        if (!result.Succeeded)
        {
            return NotFound();
        }

        return NoContent();
    }

    private IActionResult HandleFormResult(
        bool succeeded,
        IReadOnlyList<ServiceError> errors,
        UnitFormViewModel model)
    {
        if (succeeded)
        {
            return NoContent();
        }

        foreach (var error in errors)
        {
            ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
        }

        return PartialView("_UnitForm", model);
    }

    private static UnitDetails ToDetails(UnitFormViewModel model) =>
        new(
            model.UnitNumber,
            model.Bedrooms,
            model.MonthlyRent,
            model.UnitTypeId);
}
