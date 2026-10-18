using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Web.Controllers;

// The property manager's view of all applications.
[Authorize(Roles = Roles.PropertyManager)]
public class ManageApplicationsController(ApplicationListService listService)
    : Controller
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Default review queue: Submitted, oldest submission first, 20 per page.
    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        int? propertyId,
        ApplicationSortField sort = ApplicationSortField.Submitted,
        bool desc = false,
        int page = 1)
    {
        var statusFilter = ApplicationListViewModel.ParseStatus(
            status,
            defaultStatus: RentalApplicationStatus.Submitted);

        if (!Enum.IsDefined(sort))
        {
            sort = ApplicationSortField.Submitted;
        }

        var result = await listService.GetManagerApplicationsAsync(
            CurrentUserId,
            new ApplicationListQuery(statusFilter, propertyId, sort, desc, page));

        return View(ApplicationListViewModel.Create(
            result,
            statusFilter,
            propertyId,
            sort,
            desc,
            await listService.GetManagerPropertyOptionsAsync(),
            showApplicant: true,
            listController: "ManageApplications",
            detailsController: "ManageApplications"));
    }
}
