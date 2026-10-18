using System;

namespace PropertyManagement.Domain.Entities;

public class RentalApplication
{
    public int Id { get; set; }

    public int ApplicantId { get; set; }
    public Applicant Applicant { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public RentalApplicationStatus Status { get; set; }
        = RentalApplicationStatus.Draft;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ApplicantInformationSnapshot ApplicantInformation { get; set; }
        = null!;

    public ICollection<ResidenceHistoryEntry> ResidenceHistory { get; set; }
        = new List<ResidenceHistoryEntry>();

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; }
        = new List<ApplicationStatusHistory>();

    public bool ApplicantInformationCompleted { get; set; }

    public bool ResidenceHistoryCompleted { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public bool CanTransitionTo(RentalApplicationStatus newStatus)
    {
        return Status switch
        {
            RentalApplicationStatus.Draft =>
                newStatus is RentalApplicationStatus.Submitted
                    or RentalApplicationStatus.Withdrawn,

            RentalApplicationStatus.Submitted =>
                newStatus is RentalApplicationStatus.Returned
                    or RentalApplicationStatus.Approved
                    or RentalApplicationStatus.Denied
                    or RentalApplicationStatus.Withdrawn,

            RentalApplicationStatus.Returned =>
                newStatus is RentalApplicationStatus.Submitted
                    or RentalApplicationStatus.Withdrawn,

            RentalApplicationStatus.Approved
                or RentalApplicationStatus.Denied
                or RentalApplicationStatus.Withdrawn => false,

            _ => false
        };
    }

    public void TransitionTo(RentalApplicationStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition an application from {Status} to {newStatus}.");
        }

        Status = newStatus;
    }
}
