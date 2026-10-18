
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Web.Controllers;

[Authorize]
public class ApplicationsController(
    RentalApplicationService applicationService,
    RentalApplicationViewModelFactory viewModelFactory)
    : Controller
{
    public async Task<IActionResult> Details(int id)
    {
        var applicantUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(applicantUserId))
        {
            return Challenge();
        }

        var application =
            await applicationService.FindApplicantApplicationAsync(
                id,
                applicantUserId);

        if (application is null)
        {
            return NotFound();
        }

        var viewModel = viewModelFactory.Create(
            application,
            RentalApplicationSection.Summary,
            isReadOnly: application.Status !=
                PropertyManagement.Domain.Entities.RentalApplicationStatus.Draft);

        return View(viewModel);
    }
}
