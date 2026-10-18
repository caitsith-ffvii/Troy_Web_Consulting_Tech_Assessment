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
    RentalApplicationService applicationService,
    ApplicationNoteService noteService,
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

        // Null when this manager is the application's applicant.
        var notes = await noteService.GetNotesAsync(id, CurrentUserId);

        var lease = application.Status == RentalApplicationStatus.Approved
            ? await applicationService.FindLeaseForApplicationAsync(id)
            : null;

        return View(new ManagerApplicationDetailsViewModel
        {
            Application = viewModelFactory.Create(
                application,
                RentalApplicationSection.Summary,
                isReadOnly: true),
            PropertyName = application.Unit.Property.Name,
            UnitNumber = application.Unit.UnitNumber,
            MonthlyRent = application.Unit.MonthlyRent,
            AgreedRent = lease?.AgreedRent,
            LeaseStartDate = lease?.StartDate,
            LeaseEndDate = lease?.EndDate,
            AssignedManagerEmail =
                application.Unit.Property.PropertyManager.User.Email ?? string.Empty,
            CanReview = CanReview(application),
            ShowNotes = notes is not null,
            Notes = new NotesListViewModel
            {
                ApplicationId = id,
                Notes = notes ?? []
            }
        });
    }

    // Notes. Same modal conventions as the review: the partial when it needs
    // re-rendering, 204 on success, then the notes list refreshes. The
    // service re-checks access on every call.

    [HttpGet]
    public async Task<IActionResult> Notes(int id)
    {
        var notes = await noteService.GetNotesAsync(id, CurrentUserId);

        if (notes is null)
        {
            return NotFound();
        }

        return PartialView("_NotesList", new NotesListViewModel
        {
            ApplicationId = id,
            Notes = notes
        });
    }

    [HttpGet]
    public async Task<IActionResult> AddNote(int id)
    {
        if (!await noteService.CanViewNotesAsync(id, CurrentUserId))
        {
            return NotFound();
        }

        return PartialView("_NoteForm", new NoteFormViewModel { ApplicationId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int id, NoteFormViewModel model)
    {
        model.ApplicationId = id;
        model.NoteId = null;

        if (!ModelState.IsValid)
        {
            return PartialView("_NoteForm", model);
        }

        var result = await noteService.AddAsync(id, CurrentUserId, model.Body);

        return HandleNoteResult(result, model);
    }

    [HttpGet]
    public async Task<IActionResult> EditNote(int id, int noteId)
    {
        var note = await noteService.FindOwnNoteAsync(id, noteId, CurrentUserId);

        if (note is null)
        {
            return NotFound();
        }

        return PartialView("_NoteForm", new NoteFormViewModel
        {
            ApplicationId = id,
            NoteId = noteId,
            Body = note.Body
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNote(
        int id,
        int noteId,
        NoteFormViewModel model)
    {
        model.ApplicationId = id;
        model.NoteId = noteId;

        if (!ModelState.IsValid)
        {
            return PartialView("_NoteForm", model);
        }

        var result = await noteService.UpdateAsync(
            id,
            noteId,
            CurrentUserId,
            model.Body);

        return HandleNoteResult(result, model);
    }

    private IActionResult HandleNoteResult(
        NoteResult result,
        NoteFormViewModel model)
    {
        if (result.Succeeded)
        {
            return NoContent();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
        }

        return PartialView("_NoteForm", model);
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

    // The review queue: the grid loads its rows from api/applications.
    [HttpGet]
    public IActionResult Index() => View();
}
