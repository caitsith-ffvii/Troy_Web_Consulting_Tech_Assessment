
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Web.Services;

public class RentalApplicationService(ApplicationDbContext context)
{
    private const int LeaseTermMonths = 12;

    public async Task<ChangeApplicationStatusResult> ChangeStatusAsync(
        int applicationId,
        string changedByUserId,
        RentalApplicationStatus newStatus,
        string? comment)
    {
        var application = await context.RentalApplications
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
                or RentalApplicationStatus.Denied)
            && string.IsNullOrWhiteSpace(comment))
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

        await context.SaveChangesAsync();

        return ChangeApplicationStatusResult.Success();
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
