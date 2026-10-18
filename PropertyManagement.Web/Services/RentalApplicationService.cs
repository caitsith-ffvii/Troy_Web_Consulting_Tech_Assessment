using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Web.Services;

public class RentalApplicationService(ApplicationDbContext context)
{
    private const int LeaseTermMonths = 12;

    private const string ConcurrencyConflictMessage =
        "This application was changed by someone else. Reload and try again.";

    public async Task<CreateDraftApplicationResult> CreateDraftAsync(
        string applicantUserId,
        int unitId)
    {
        var applicant = await context.Applicants
            .Include(a => a.User)
            .SingleOrDefaultAsync(a => a.UserId == applicantUserId);

        if (applicant is null)
        {
            return CreateDraftApplicationResult.Failure(
                "Applicant profile not found.");
        }

        var unit = await context.Units
            .Include(u => u.Property)
            .SingleOrDefaultAsync(u => u.Id == unitId);

        if (unit is null)
        {
            return CreateDraftApplicationResult.Failure(
                "Unit not found.");
        }

        if (unit.IsArchived || unit.Property.IsArchived)
        {
            return CreateDraftApplicationResult.Failure(
                "Applications cannot be created for archived units or properties.");
        }

        // Applying again for the same unit resumes the open application
        // instead of creating a duplicate.
        var openApplication = await context.RentalApplications
            .Where(a =>
                a.ApplicantId == applicant.Id &&
                a.UnitId == unitId &&
                (a.Status == RentalApplicationStatus.Draft ||
                 a.Status == RentalApplicationStatus.Submitted ||
                 a.Status == RentalApplicationStatus.Returned))
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        if (openApplication is not null)
        {
            return CreateDraftApplicationResult.Success(openApplication);
        }

        var today = DateTime.UtcNow.Date;

        var hasActiveLease = await context.Leases
            .AnyAsync(l =>
                l.UnitId == unitId &&
                l.StartDate <= today &&
                l.EndDate >= today);

        if (hasActiveLease)
        {
            return CreateDraftApplicationResult.Failure(
                "This unit is not currently available.");
        }

        var now = DateTime.UtcNow;

        var application = new RentalApplication
        {
            ApplicantId = applicant.Id,
            UnitId = unit.Id,
            Status = RentalApplicationStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ApplicantInformation = new ApplicantInformationSnapshot
            {
                FirstName = applicant.FirstName,
                LastName = applicant.LastName,
                PhoneNumber = applicant.PhoneNumber,
                Email = applicant.User.Email ?? string.Empty,
                CurrentAddress = applicant.CurrentAddress
            }
        };

        context.RentalApplications.Add(application);
        await context.SaveChangesAsync();

        return CreateDraftApplicationResult.Success(application);
    }

    public async Task<RentalApplication?> FindApplicantApplicationAsync(
        int applicationId,
        string applicantUserId)
    {
        return await context.RentalApplications
            .Include(application => application.ApplicantInformation)
            .Include(application => application.ResidenceHistory)
            .Where(application =>
                application.Id == applicationId &&
                application.Applicant.UserId == applicantUserId)
            .SingleOrDefaultAsync();
    }

    public async Task<ChangeApplicationStatusResult> SaveApplicantInformationAsync(
        int applicationId,
        string applicantUserId,
        ApplicantInformationSnapshot information)
    {
        ArgumentNullException.ThrowIfNull(information);

        var result = await GetEditableApplicationAsync(
            applicationId,
            applicantUserId);

        if (!result.Succeeded)
        {
            return ChangeApplicationStatusResult.Failure(result.Error!);
        }

        if (string.IsNullOrWhiteSpace(information.FirstName) ||
            string.IsNullOrWhiteSpace(information.LastName) ||
            string.IsNullOrWhiteSpace(information.PhoneNumber) ||
            string.IsNullOrWhiteSpace(information.Email) ||
            string.IsNullOrWhiteSpace(information.CurrentAddress))
        {
            return ChangeApplicationStatusResult.Failure(
                "All applicant information fields are required.");
        }

        var application = result.Application!;

        await context.Entry(application)
            .Reference(a => a.ApplicantInformation)
            .LoadAsync();

        application.ApplicantInformation.FirstName = information.FirstName;
        application.ApplicantInformation.LastName = information.LastName;
        application.ApplicantInformation.PhoneNumber = information.PhoneNumber;
        application.ApplicantInformation.Email = information.Email;
        application.ApplicantInformation.CurrentAddress = information.CurrentAddress;
        application.ApplicantInformationCompleted = true;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        return ChangeApplicationStatusResult.Success();
    }

    public async Task<ChangeApplicationStatusResult> AddResidenceHistoryAsync(
        int applicationId,
        string applicantUserId,
        ResidenceHistoryEntry entry)
    {
        var result = await GetEditableApplicationAsync(
            applicationId,
            applicantUserId);

        if (!result.Succeeded)
        {
            return ChangeApplicationStatusResult.Failure(result.Error!);
        }

        var dateError = ValidateResidenceHistoryDates(
            entry.MoveInDate,
            entry.MoveOutDate);

        if (dateError is not null)
        {
            return ChangeApplicationStatusResult.Failure(dateError);
        }

        var application = result.Application!;

        application.ResidenceHistory.Add(new ResidenceHistoryEntry
        {
            Address = entry.Address,
            LandlordName = entry.LandlordName,
            LandlordPhoneNumber = entry.LandlordPhoneNumber,
            MoveInDate = entry.MoveInDate,
            MoveOutDate = entry.MoveOutDate
        });

        application.ResidenceHistoryCompleted = false;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        return ChangeApplicationStatusResult.Success();
    }

    public async Task<ChangeApplicationStatusResult> UpdateResidenceHistoryAsync(
        int applicationId,
        string applicantUserId,
        int residenceHistoryEntryId,
        ResidenceHistoryEntry updatedEntry)
    {
        var result = await GetEditableApplicationAsync(
            applicationId,
            applicantUserId);

        if (!result.Succeeded)
        {
            return ChangeApplicationStatusResult.Failure(result.Error!);
        }

        var entry = await context.ResidenceHistoryEntries
            .SingleOrDefaultAsync(e =>
                e.Id == residenceHistoryEntryId &&
                e.RentalApplicationId == applicationId);

        if (entry is null)
        {
            return ChangeApplicationStatusResult.Failure(
                "Residence history entry not found.");
        }

        var dateError = ValidateResidenceHistoryDates(
            updatedEntry.MoveInDate,
            updatedEntry.MoveOutDate);

        if (dateError is not null)
        {
            return ChangeApplicationStatusResult.Failure(dateError);
        }

        entry.Address = updatedEntry.Address;
        entry.LandlordName = updatedEntry.LandlordName;
        entry.LandlordPhoneNumber = updatedEntry.LandlordPhoneNumber;
        entry.MoveInDate = updatedEntry.MoveInDate;
        entry.MoveOutDate = updatedEntry.MoveOutDate;

        var application = result.Application!;
        application.ResidenceHistoryCompleted = false;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        return ChangeApplicationStatusResult.Success();
    }

    public async Task<ChangeApplicationStatusResult> RemoveResidenceHistoryAsync(
        int applicationId,
        string applicantUserId,
        int residenceHistoryEntryId)
    {
        var result = await GetEditableApplicationAsync(
            applicationId,
            applicantUserId);

        if (!result.Succeeded)
        {
            return ChangeApplicationStatusResult.Failure(result.Error!);
        }

        var entry = await context.ResidenceHistoryEntries
            .SingleOrDefaultAsync(e =>
                e.Id == residenceHistoryEntryId &&
                e.RentalApplicationId == applicationId);

        if (entry is null)
        {
            return ChangeApplicationStatusResult.Failure(
                "Residence history entry not found.");
        }

        context.ResidenceHistoryEntries.Remove(entry);

        var application = result.Application!;
        application.ResidenceHistoryCompleted = false;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        return ChangeApplicationStatusResult.Success();
    }

    public async Task<ChangeApplicationStatusResult> CompleteResidenceHistoryAsync(
        int applicationId,
        string applicantUserId)
    {
        var result = await GetEditableApplicationAsync(
            applicationId,
            applicantUserId);

        if (!result.Succeeded)
        {
            return ChangeApplicationStatusResult.Failure(result.Error!);
        }

        var application = result.Application!;

        var entries = await context.ResidenceHistoryEntries
            .Where(e => e.RentalApplicationId == applicationId)
            .ToListAsync();

        var hasInvalidEntry = entries.Any(entry =>
            string.IsNullOrWhiteSpace(entry.Address) ||
            string.IsNullOrWhiteSpace(entry.LandlordName) ||
            string.IsNullOrWhiteSpace(entry.LandlordPhoneNumber) ||
            ValidateResidenceHistoryDates(
                entry.MoveInDate,
                entry.MoveOutDate) is not null);

        if (hasInvalidEntry)
        {
            return ChangeApplicationStatusResult.Failure(
                "Residence history entries contain missing required fields or invalid dates.");
        }

        application.ResidenceHistoryCompleted = true;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        return ChangeApplicationStatusResult.Success();
    }

    public async Task<ChangeApplicationStatusResult> SubmitAsync(
        int applicationId,
        string applicantUserId)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var application = await context.RentalApplications
            .Include(a => a.Unit)
                .ThenInclude(u => u.Property)
            .SingleOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return ChangeApplicationStatusResult.Failure(
                "Application not found.");
        }

        var ownsApplication = await context.RentalApplications
            .AnyAsync(a =>
                a.Id == applicationId &&
                a.Applicant.UserId == applicantUserId);

        if (!ownsApplication)
        {
            return ChangeApplicationStatusResult.Failure(
                "You are not authorized to submit this application.");
        }

        if (application.Unit.IsArchived || application.Unit.Property.IsArchived)
        {
            return ChangeApplicationStatusResult.Failure(
                "Applications for archived units or properties cannot be submitted.");
        }

        if (!application.ApplicantInformationCompleted ||
            !application.ResidenceHistoryCompleted)
        {
            return ChangeApplicationStatusResult.Failure(
                "Both application sections must be completed before submission.");
        }

        if (!application.CanTransitionTo(RentalApplicationStatus.Submitted))
        {
            return ChangeApplicationStatusResult.Failure(
                $"Cannot submit an application from {application.Status}.");
        }

        var today = DateTime.UtcNow.Date;

        var hasActiveLease = await context.Leases
            .AnyAsync(l =>
                l.UnitId == application.UnitId &&
                l.StartDate <= today &&
                l.EndDate >= today);

        if (hasActiveLease)
        {
            return ChangeApplicationStatusResult.Failure(
                "This unit already has an active lease.");
        }

        var now = DateTime.UtcNow;
        var previousStatus = application.Status;

        application.TransitionTo(RentalApplicationStatus.Submitted);
        application.UpdatedAtUtc = now;

        context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            RentalApplicationId = application.Id,
            ChangedByUserId = applicantUserId,
            ChangedAtUtc = now,
            PreviousStatus = previousStatus,
            NewStatus = RentalApplicationStatus.Submitted,
            Comment = null
        });

        // On conflict the undisposed transaction rolls back, so approval
        // never leaves a lease behind.
        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        await transaction.CommitAsync();

        return ChangeApplicationStatusResult.Success();
    }

    public async Task<ChangeApplicationStatusResult> WithdrawAsync(
        int applicationId,
        string applicantUserId)
    {
        var application = await context.RentalApplications
            .Include(a => a.Applicant)
            .SingleOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return ChangeApplicationStatusResult.Failure(
                "Application not found.");
        }

        if (application.Applicant.UserId != applicantUserId)
        {
            return ChangeApplicationStatusResult.Failure(
                "You are not authorized to withdraw this application.");
        }

        if (!application.CanTransitionTo(RentalApplicationStatus.Withdrawn))
        {
            return ChangeApplicationStatusResult.Failure(
                $"Cannot withdraw an application from {application.Status}.");
        }

        var now = DateTime.UtcNow;
        var previousStatus = application.Status;

        application.TransitionTo(RentalApplicationStatus.Withdrawn);
        application.UpdatedAtUtc = now;

        context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            RentalApplicationId = application.Id,
            ChangedByUserId = applicantUserId,
            ChangedAtUtc = now,
            PreviousStatus = previousStatus,
            NewStatus = RentalApplicationStatus.Withdrawn,
            Comment = null
        });

        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        return ChangeApplicationStatusResult.Success();
    }

    public async Task<ChangeApplicationStatusResult> ChangeStatusAsync(
        int applicationId,
        string changedByUserId,
        RentalApplicationStatus newStatus,
        string? comment)
    {
        // Withdrawal and (re)submission belong to the applicant.
        if (newStatus is not (
            RentalApplicationStatus.Approved or
            RentalApplicationStatus.Returned or
            RentalApplicationStatus.Denied))
        {
            return ChangeApplicationStatusResult.Failure(
                "Property managers can only approve, return, or deny applications.");
        }

        // Approval needs a transaction so the unit lock, lease check,
        // application update, and lease creation are atomic.
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var application = await context.RentalApplications
            .Include(a => a.Unit)
                .ThenInclude(u => u.Property)
            .SingleOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return ChangeApplicationStatusResult.Failure(
                "Application not found.");
        }

        var ownsProperty = await context.Units
            .AnyAsync(u =>
                u.Id == application.UnitId &&
                u.Property.PropertyManager.UserId == changedByUserId);

        if (!ownsProperty)
        {
            return ChangeApplicationStatusResult.Failure(
                "You are not authorized to manage this application's property.");
        }

        if ((newStatus is RentalApplicationStatus.Returned
                or RentalApplicationStatus.Denied) &&
            string.IsNullOrWhiteSpace(comment))
        {
            return ChangeApplicationStatusResult.Failure(
                "A comment is required when returning or denying an application.");
        }

        if (!application.CanTransitionTo(newStatus))
        {
            return ChangeApplicationStatusResult.Failure(
                $"Cannot transition an application from {application.Status} to {newStatus}.");
        }

        var now = DateTime.UtcNow;

        if (newStatus == RentalApplicationStatus.Approved)
        {
            if (application.Unit.IsArchived ||
                application.Unit.Property.IsArchived)
            {
                return ChangeApplicationStatusResult.Failure(
                    "Archived units or properties cannot receive a new lease.");
            }

            // Serialize approval attempts targeting the same unit.
            // SQL Server holds this update lock until the transaction ends.
            var lockedUnitIds = await context.Units
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM dbo.Units WITH (UPDLOCK, HOLDLOCK)
                    WHERE Id = {application.UnitId}
                    """)
                .Select(u => u.Id)
                .ToListAsync();

            if (lockedUnitIds.Count == 0)
            {
                return ChangeApplicationStatusResult.Failure(
                    "The unit could not be found.");
            }

            var leaseStartDate = now.Date;
            var leaseEndDate = leaseStartDate.AddMonths(LeaseTermMonths);

            var hasOverlappingLease = await context.Leases
                .AnyAsync(l =>
                    l.UnitId == application.UnitId &&
                    l.StartDate <= leaseEndDate &&
                    l.EndDate >= leaseStartDate);

            if (hasOverlappingLease)
            {
                return ChangeApplicationStatusResult.Failure(
                    "This unit already has a lease that overlaps the proposed lease period.");
            }

            context.Leases.Add(new Lease
            {
                UnitId = application.UnitId,
                ApplicantId = application.ApplicantId,
                StartDate = leaseStartDate,
                EndDate = leaseEndDate,
                AgreedRent = application.Unit.MonthlyRent
            });
        }

        var previousStatus = application.Status;

        application.TransitionTo(newStatus);
        application.UpdatedAtUtc = now;

        context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            RentalApplicationId = application.Id,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = now,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Comment = string.IsNullOrWhiteSpace(comment)
                ? null
                : comment.Trim()
        });

        // On conflict the undisposed transaction rolls back, so approval
        // never leaves a lease behind.
        if (!await TrySaveChangesAsync())
        {
            return ChangeApplicationStatusResult.Failure(
                ConcurrencyConflictMessage);
        }

        await transaction.CommitAsync();

        return ChangeApplicationStatusResult.Success();
    }

    // RentalApplication.RowVersion makes a stale write fail rather than
    // overwrite a concurrent change.
    private async Task<bool> TrySaveChangesAsync()
    {
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static string? ValidateResidenceHistoryDates(
        DateTime moveInDate,
        DateTime? moveOutDate)
    {
        if (moveInDate.Date > DateTime.Today)
        {
            return "Move-in date cannot be in the future.";
        }

        if (moveOutDate.HasValue &&
            moveOutDate.Value.Date < moveInDate.Date)
        {
            return "Move-out date cannot be before move-in date.";
        }

        return null;
    }

    private async Task<EditableApplicationResult> GetEditableApplicationAsync(
        int applicationId,
        string applicantUserId)
    {
        var application = await context.RentalApplications
            .Include(a => a.Applicant)
            .SingleOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return EditableApplicationResult.Failure("Application not found.");
        }

        if (application.Applicant.UserId != applicantUserId)
        {
            return EditableApplicationResult.Failure(
                "You are not authorized to edit this application.");
        }

        if (application.Status is not (
            RentalApplicationStatus.Draft or
            RentalApplicationStatus.Returned))
        {
            return EditableApplicationResult.Failure(
                "This application can no longer be edited.");
        }

        return EditableApplicationResult.Success(application);
    }
}

public sealed record ChangeApplicationStatusResult(
    bool Succeeded,
    string? Error)
{
    public static ChangeApplicationStatusResult Success() =>
        new(true, null);

    public static ChangeApplicationStatusResult Failure(string error) =>
        new(false, error);
}

public sealed record CreateDraftApplicationResult(
    bool Succeeded,
    RentalApplication? Application,
    string? Error)
{
    public static CreateDraftApplicationResult Success(
        RentalApplication application) =>
        new(true, application, null);

    public static CreateDraftApplicationResult Failure(string error) =>
        new(false, null, error);
}

public sealed record EditableApplicationResult(
    RentalApplication? Application,
    string? Error)
{
    public bool Succeeded => Application is not null;

    public static EditableApplicationResult Success(
        RentalApplication application) =>
        new(application, null);

    public static EditableApplicationResult Failure(string error) =>
        new(null, error);
}
