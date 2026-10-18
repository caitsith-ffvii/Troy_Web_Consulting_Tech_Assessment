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
public class ManageApplicationsController(
    ApplicationListService listService,
    RentalApplicationService applicationService,
    RentalApplicationViewModelFactory viewModelFactory)
    : Controller
{
    // Any manager can view; only the assigned manager can review.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var application = await applicationService.FindManagerApplicationAsync(
            id,
            CurrentUserId);

        if (application is null)
        {
            return NotFound();
        }

        return View(new ManagerApplicationDetailsViewModel
        {
            Application = viewModelFactory.Create(
                application,
                RentalApplicationSection.Summary,
                isReadOnly: true),
            PropertyName = application.Unit.Property.Name,
            UnitNumber = application.Unit.UnitNumber,
            MonthlyRent = application.Unit.MonthlyRent,
            AssignedManagerEmail =
                application.Unit.Property.PropertyManager.User.Email ?? string.Empty,
            CanReview = CanReview(application)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var application = await applicationService.FindManagerApplicationAsync(
            id,
            CurrentUserId);

        if (application is null || !CanReview(application))
        {
            return NotFound();
        }

        return PartialView("_ReviewForm", new ReviewFormViewModel
        {
            ApplicationId = id,
            MonthlyRent = application.Unit.MonthlyRent
        });
    }

    // Returns the form on validation or business failure, 204 on success
    // (the modal script then reloads the page).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, ReviewFormViewModel model)
    {
        var application = await applicationService.FindManagerApplicationAsync(
            id,
            CurrentUserId);

        if (application is null)
        {
            return NotFound();
        }

        model.ApplicationId = id;
        model.MonthlyRent = application.Unit.MonthlyRent;

        if (!ModelState.IsValid)
        {
            return PartialView("_ReviewForm", model);
        }

        // The service re-checks assignment, status, comment, and leases.
        var result = await applicationService.ChangeStatusAsync(
            id,
            CurrentUserId,
            model.NewStatus!.Value,
            model.Comment);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return PartialView("_ReviewForm", model);
        }

        return NoContent();
    }

    private bool CanReview(RentalApplication application) =>
        application.Status == RentalApplicationStatus.Submitted &&
        application.Unit.Property.PropertyManager.UserId == CurrentUserId;

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
