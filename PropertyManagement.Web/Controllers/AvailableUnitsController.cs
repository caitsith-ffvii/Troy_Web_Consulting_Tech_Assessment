using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.Properties;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = Roles.Applicant)]
public class AvailableUnitsController(UnitService unitService)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var units = await unitService.GetAvailableUnitsAsync();

        return View(units
            .Select(u => new AvailableUnitViewModel
            {
                UnitId = u.Id,
                PropertyName = u.Property.Name,
                PropertyAddress = u.Property.Address,
                UnitNumber = u.UnitNumber,
                UnitTypeName = u.UnitType.Name,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent
            })
            .ToList());
    }
}
