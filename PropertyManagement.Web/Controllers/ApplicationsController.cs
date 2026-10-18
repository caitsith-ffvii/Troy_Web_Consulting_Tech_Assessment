
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.Properties;
using PropertyManagement.Web.ViewModels.RentalApplications;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = Roles.Applicant)]
public class ApplicationsController(
    RentalApplicationService applicationService,
    ApplicationListService listService,
    RentalApplicationViewModelFactory viewModelFactory)
    : Controller
{
    // "My applications": all statuses, most recently submitted first.
    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        int? propertyId,
        ApplicationSortField sort = ApplicationSortField.Submitted,
        bool desc = true,
        int page = 1)
    {
        var statusFilter = ApplicationListViewModel.ParseStatus(status, defaultStatus: null);

        if (!Enum.IsDefined(sort))
        {
            sort = ApplicationSortField.Submitted;
        }

        var result = await listService.GetApplicantApplicationsAsync(
            CurrentUserId,
            new ApplicationListQuery(statusFilter, propertyId, sort, desc, page));

        return View(ApplicationListViewModel.Create(
            result,
            statusFilter,
            propertyId,
            sort,
            desc,
            await listService.GetApplicantPropertyOptionsAsync(CurrentUserId),
            showApplicant: false,
            listController: "Applications",
            detailsController: "Applications"));
    }

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
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "AvailableUnits");
        }

        return RedirectToAction(
            nameof(Details),
            new { id = result.Application!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Withdraw(int id)
    {
        var application = await applicationService.FindApplicantApplicationAsync(
            id,
            CurrentUserId);

        if (application is null ||
            !application.CanTransitionTo(RentalApplicationStatus.Withdrawn))
        {
            return NotFound();
        }

        return PartialView("_RemoveConfirm", WithdrawConfirmModel(id));
    }

    // 204 on success; the modal script then reloads the page.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Withdraw))]
    public async Task<IActionResult> WithdrawConfirmed(int id)
    {
        var result = await applicationService.WithdrawAsync(id, CurrentUserId);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return PartialView("_RemoveConfirm", WithdrawConfirmModel(id));
        }

        return NoContent();
    }

    private RemoveConfirmViewModel WithdrawConfirmModel(int id) =>
        new()
        {
            Title = $"Withdraw application #{id}",
            Message = "Withdrawing is final: the application can no longer be edited or submitted. You can apply for the unit again later.",
            ActionUrl = Url.Action(nameof(Withdraw), new { id })!,
            ConfirmLabel = "Withdraw"
        };

    // Residence history modals. Each action returns its partial when it
    // needs re-rendering and 204 No Content on success, which closes the
    // modal and refreshes #residence-history-list.

    [HttpGet]
    public async Task<IActionResult> ResidenceHistory(int id)
    {
        var application = await applicationService.FindApplicantApplicationAsync(
            id,
            CurrentUserId);

        if (application is null)
        {
            return NotFound();
        }

        var viewModel = viewModelFactory.Create(
            application,
            RentalApplicationSection.ResidenceHistory,
            !IsEditableStatus(application.Status));

        return PartialView("_ResidenceHistoryList", new ResidenceHistoryListViewModel
        {
            ApplicationId = id,
            IsReadOnly = viewModel.IsReadOnly,
            Entries = viewModel.ResidenceHistory
        });
    }

    [HttpGet]
    public async Task<IActionResult> AddResidence(int id)
    {
        if (await FindEditableApplicationAsync(id) is null)
        {
            return NotFound();
        }

        return ResidenceForm(id, new ResidenceHistoryEntryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResidence(
        int id,
        ResidenceHistoryEntryViewModel model)
    {
        model.Id = 0;

        if (!ModelState.IsValid)
        {
            return ResidenceForm(id, model);
        }

        var result = await applicationService.AddResidenceHistoryAsync(
            id,
            CurrentUserId,
            ToEntry(model));

        return HandleResidenceResult(result, id, model);
    }

    [HttpGet]
    public async Task<IActionResult> EditResidence(int id, int entryId)
    {
        var application = await FindEditableApplicationAsync(id);
        var entry = application?.ResidenceHistory.SingleOrDefault(e => e.Id == entryId);

        if (entry is null)
        {
            return NotFound();
        }

        return ResidenceForm(id, new ResidenceHistoryEntryViewModel
        {
            Id = entry.Id,
            Address = entry.Address,
            LandlordName = entry.LandlordName,
            LandlordPhoneNumber = entry.LandlordPhoneNumber,
            MoveInDate = entry.MoveInDate,
            MoveOutDate = entry.MoveOutDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditResidence(
        int id,
        int entryId,
        ResidenceHistoryEntryViewModel model)
    {
        model.Id = entryId;

        if (!ModelState.IsValid)
        {
            return ResidenceForm(id, model);
        }

        var result = await applicationService.UpdateResidenceHistoryAsync(
            id,
            CurrentUserId,
            entryId,
            ToEntry(model));

        return HandleResidenceResult(result, id, model);
    }

    [HttpGet]
    public async Task<IActionResult> RemoveResidence(int id, int entryId)
    {
        var application = await FindEditableApplicationAsync(id);
        var entry = application?.ResidenceHistory.SingleOrDefault(e => e.Id == entryId);

        if (entry is null)
        {
            return NotFound();
        }

        return PartialView("_RemoveConfirm", new RemoveConfirmViewModel
        {
            Title = "Remove residence",
            Message = $"Remove {entry.Address} from your residence history?",
            ActionUrl = Url.Action(nameof(RemoveResidence), new { id, entryId })!
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(RemoveResidence))]
    public async Task<IActionResult> RemoveResidenceConfirmed(int id, int entryId)
    {
        var result = await applicationService.RemoveResidenceHistoryAsync(
            id,
            CurrentUserId,
            entryId);

        return result.Succeeded ? NoContent() : NotFound();
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Ownership and editable status come from the database, never the form.
    private async Task<RentalApplication?> FindEditableApplicationAsync(int id)
    {
        var application = await applicationService.FindApplicantApplicationAsync(
            id,
            CurrentUserId);

        return application is not null && IsEditableStatus(application.Status)
            ? application
            : null;
    }

    private PartialViewResult ResidenceForm(
        int applicationId,
        ResidenceHistoryEntryViewModel model)
    {
        ViewData["ApplicationId"] = applicationId;
        return PartialView("_ResidenceForm", model);
    }

    private IActionResult HandleResidenceResult(
        ChangeApplicationStatusResult result,
        int applicationId,
        ResidenceHistoryEntryViewModel model)
    {
        if (result.Succeeded)
        {
            return NoContent();
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        return ResidenceForm(applicationId, model);
    }

    private static ResidenceHistoryEntry ToEntry(
        ResidenceHistoryEntryViewModel model) =>
        new()
        {
            Address = model.Address.Trim(),
            LandlordName = model.LandlordName.Trim(),
            LandlordPhoneNumber = model.LandlordPhoneNumber.Trim(),
            MoveInDate = model.MoveInDate.Date,
            MoveOutDate = model.MoveOutDate?.Date
        };

    private static bool IsEditableStatus(RentalApplicationStatus status) =>
        status is RentalApplicationStatus.Draft or RentalApplicationStatus.Returned;
}
