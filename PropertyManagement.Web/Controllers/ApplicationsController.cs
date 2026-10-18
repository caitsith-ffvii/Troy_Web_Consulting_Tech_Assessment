
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Web.Controllers;

[Authorize]
public class ApplicationsController(
    RentalApplicationService applicationService,
    RentalApplicationViewModelFactory viewModelFactory)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        RentalApplicationSection? section = null)
    {
        var applicantUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(applicantUserId))
        {
            return Challenge();
        }

        var application = await applicationService.FindApplicantApplicationAsync(id, applicantUserId);

        if (application is null)
        {
            return NotFound();
        }

        var isReadOnly = !IsEditableStatus(application.Status);

        var requestedSection = section ?? (
            application.ApplicantInformationCompleted
                ? RentalApplicationSection.ResidenceHistory
                : RentalApplicationSection.ApplicantInformation);

        if (!Enum.IsDefined(requestedSection))
        {
            return BadRequest();
        }

        // Read-only applications always open on their summary.
        if (isReadOnly)
        {
            requestedSection = RentalApplicationSection.Summary;
        }

        var viewModel = viewModelFactory.Create(application, requestedSection, isReadOnly);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Navigate(
        int id,
        RentalApplicationViewModel model,
        string command)
    {
        var applicantUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(applicantUserId))
        {
            return Challenge();
        }

        var application = await applicationService.FindApplicantApplicationAsync(id, applicantUserId);

        if (application is null)
        {
            return NotFound();
        }

        // Determine editability from persisted state, never posted flags.
        if (!IsEditableStatus(application.Status))
        {
            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        if (!Enum.IsDefined(model.Section))
        {
            return BadRequest();
        }

        switch (command)
        {
            case "Back":
            {
                var previousSection = model.Section switch
                {
                    RentalApplicationSection.ResidenceHistory =>
                        RentalApplicationSection.ApplicantInformation,

                    RentalApplicationSection.Summary =>
                        RentalApplicationSection.ResidenceHistory,

                    _ => (RentalApplicationSection?)null
                };

                if (previousSection is null)
                {
                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id,
                            section = RentalApplicationSection.ApplicantInformation
                        });
                }

                // Back deliberately does not save posted values.
                return RedirectToAction(
                    nameof(Details),
                    new { id, section = previousSection });
            }

            case "Continue":
            {
                ChangeApplicationStatusResult result;

                switch (model.Section)
                {
                    case RentalApplicationSection.ApplicantInformation:
                    {                        
                        if (!ModelState.IsValid)
                        {
                            var invalidModel = viewModelFactory.Create(
                                application,
                                RentalApplicationSection.ApplicantInformation,
                                isReadOnly: false);

                            // Preserve the submitted values and MVC field-level errors.
                            invalidModel.ApplicantInformation = model.ApplicantInformation;

                            return View("Details", invalidModel);
                        }

                        var information = new ApplicantInformationSnapshot
                        {
                            FirstName = model.ApplicantInformation.FirstName,
                            LastName = model.ApplicantInformation.LastName,
                            PhoneNumber = model.ApplicantInformation.PhoneNumber,
                            Email = model.ApplicantInformation.Email,
                            CurrentAddress = model.ApplicantInformation.CurrentAddress
                        };

                        result =
                            await applicationService.SaveApplicantInformationAsync(
                                id,
                                applicantUserId,
                                information);

                        if (!result.Succeeded)
                        {
                            ModelState.AddModelError(
                                string.Empty,
                                result.Error!);

                            var failedModel = viewModelFactory.Create(
                                application,
                                RentalApplicationSection.ApplicantInformation,
                                isReadOnly: false);

                            // Preserve what the applicant actually entered.
                            failedModel.ApplicantInformation =
                                model.ApplicantInformation;

                            return View("Details", failedModel);
                        }

                        return RedirectToAction(
                            nameof(Details),
                            new
                            {
                                id,
                                section = RentalApplicationSection.ResidenceHistory
                            });
                    }

                    case RentalApplicationSection.ResidenceHistory:
                    {
                        result = await applicationService.CompleteResidenceHistoryAsync(
                                id,
                                applicantUserId);

                        if (!result.Succeeded)
                        {
                            ModelState.AddModelError(string.Empty, result.Error!);

                            var failedModel = viewModelFactory.Create(
                                application,
                                RentalApplicationSection.ResidenceHistory,
                                isReadOnly: false);

                            return View("Details", failedModel);
                        }

                        return RedirectToAction(
                            nameof(Details),
                            new
                            {
                                id,
                                section = RentalApplicationSection.Summary
                            });
                    }

                    case RentalApplicationSection.Summary:
                        // Summary has a Submit action, not Continue.
                        return BadRequest();

                    default:
                        return BadRequest();
                }
            }

            case "Submit":
            {
                // Submission is only allowed from the Summary section.
                if (model.Section != RentalApplicationSection.Summary)
                {
                    return BadRequest();
                }

                var result = await applicationService.SubmitAsync(id, applicantUserId);

                if (!result.Succeeded)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        result.Error!);

                    var failedModel = viewModelFactory.Create(
                        application,
                        RentalApplicationSection.Summary,
                        isReadOnly: false);

                    return View("Details", failedModel);
                }

                return RedirectToAction(nameof(Details), new { id });
            }

            default:
                return BadRequest();
        }
    }

    // Creating a draft changes server state, so this must be POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int unitId)
    {
        if (unitId <= 0)
        {
            return BadRequest();
        }

        var applicantUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(applicantUserId))
        {
            return Challenge();
        }

        var result = await applicationService.CreateDraftAsync(
            applicantUserId,
            unitId);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return BadRequest(ModelState);
        }

        return RedirectToAction(
            nameof(Details),
            new { id = result.Application!.Id });
    }

    private static bool IsEditableStatus(RentalApplicationStatus status) =>
        status is RentalApplicationStatus.Draft or RentalApplicationStatus.Returned;
}
