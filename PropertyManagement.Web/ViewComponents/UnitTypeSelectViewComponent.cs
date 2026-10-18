using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.ViewComponents;

// Renders the unit type dropdown: active types, plus the unit's current
// type when it has been deactivated. The server enforces the same rule.
public class UnitTypeSelectViewComponent(UnitService unitService)
    : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        int selectedUnitTypeId,
        int? currentUnitTypeId)
    {
        var unitTypes = await unitService.GetSelectableUnitTypesAsync(
            currentUnitTypeId);

        return View(new UnitTypeSelectViewModel(
            selectedUnitTypeId,
            unitTypes
                .Select(t => new UnitTypeOption(t.Id, t.Name, t.IsActive))
                .ToList()));
    }
}

public sealed record UnitTypeSelectViewModel(
    int SelectedUnitTypeId,
    IReadOnlyList<UnitTypeOption> Options);

public sealed record UnitTypeOption(
    int Id,
    string Name,
    bool IsActive);
