
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
}
