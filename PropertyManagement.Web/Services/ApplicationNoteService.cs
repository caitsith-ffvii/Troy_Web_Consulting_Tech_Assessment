using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Data;
using PropertyManagement.Web.Data.Configurations;

namespace PropertyManagement.Web.Services;

// Manager-only notes on applications. This service is the only way to read
// or write notes, and every operation re-checks access: the caller must be
// a property manager and must not be the application's applicant (an
// account can hold both roles).
public class ApplicationNoteService(ApplicationDbContext context)
{
    private const string NoAccessMessage =
        "Notes are only available to property managers.";

    public async Task<bool> CanViewNotesAsync(
        int applicationId,
        string userId)
    {
        var isManager = await context.PropertyManagers
            .AnyAsync(m => m.UserId == userId);

        return isManager &&
            await context.RentalApplications.AnyAsync(a =>
                a.Id == applicationId &&
                a.Applicant.UserId != userId);
    }

    // Null when the caller may not see this application's notes.
    public async Task<List<ApplicationNoteItem>?> GetNotesAsync(
        int applicationId,
        string userId)
    {
        if (!await CanViewNotesAsync(applicationId, userId))
        {
            return null;
        }

        return await context.ApplicationNotes
            .AsNoTracking()
            .Where(n => n.RentalApplicationId == applicationId)
            .OrderBy(n => n.CreatedAtUtc)
            .ThenBy(n => n.Id)
            .Select(n => new ApplicationNoteItem(
                n.Id,
                n.Body,
                context.Users
                    .Where(u => u.Id == n.AuthorUserId)
                    .Select(u => u.Email)
                    .FirstOrDefault() ?? "Unknown user",
                n.CreatedAtUtc,
                n.UpdatedAtUtc,
                n.AuthorUserId == userId))
            .ToListAsync();
    }

    // The caller's own note, for editing; null otherwise.
    public async Task<ApplicationNote?> FindOwnNoteAsync(
        int applicationId,
        int noteId,
        string userId)
    {
        if (!await CanViewNotesAsync(applicationId, userId))
        {
            return null;
        }

        return await context.ApplicationNotes
            .AsNoTracking()
            .SingleOrDefaultAsync(n =>
                n.Id == noteId &&
                n.RentalApplicationId == applicationId &&
                n.AuthorUserId == userId);
    }

    public async Task<NoteResult> AddAsync(
        int applicationId,
        string userId,
        string body)
    {
        if (!await CanViewNotesAsync(applicationId, userId))
        {
            return NoteResult.Failure(null, NoAccessMessage);
        }

        var errors = Validate(body);

        if (errors.Count > 0)
        {
            return NoteResult.Failure(errors);
        }

        var now = DateTime.UtcNow;

        context.ApplicationNotes.Add(new ApplicationNote
        {
            RentalApplicationId = applicationId,
            AuthorUserId = userId,
            Body = body.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await context.SaveChangesAsync();

        return NoteResult.Success();
    }

    // Only a note's author can edit it.
    public async Task<NoteResult> UpdateAsync(
        int applicationId,
        int noteId,
        string userId,
        string body)
    {
        if (!await CanViewNotesAsync(applicationId, userId))
        {
            return NoteResult.Failure(null, NoAccessMessage);
        }

        var note = await context.ApplicationNotes
            .SingleOrDefaultAsync(n =>
                n.Id == noteId &&
                n.RentalApplicationId == applicationId);

        if (note is null || note.AuthorUserId != userId)
        {
            return NoteResult.Failure(
                null,
                "Note not found, or you are not its author.");
        }

        var errors = Validate(body);

        if (errors.Count > 0)
        {
            return NoteResult.Failure(errors);
        }

        note.Body = body.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoteResult.Success();
    }

    private static List<ServiceError> Validate(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [new ServiceError("Body", "Note text is required.")];
        }

        if (body.Trim().Length > ApplicationNoteConfiguration.BodyMaxLength)
        {
            return
            [
                new ServiceError(
                    "Body",
                    $"Notes cannot exceed {ApplicationNoteConfiguration.BodyMaxLength} characters.")
            ];
        }

        return [];
    }
}

public sealed record ApplicationNoteItem(
    int Id,
    string Body,
    string AuthorEmail,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool CanEdit);

public sealed record NoteResult(
    bool Succeeded,
    IReadOnlyList<ServiceError> Errors)
{
    public static NoteResult Success() => new(true, []);

    public static NoteResult Failure(IReadOnlyList<ServiceError> errors) =>
        new(false, errors);

    public static NoteResult Failure(string? field, string message) =>
        new(false, [new ServiceError(field, message)]);
}
