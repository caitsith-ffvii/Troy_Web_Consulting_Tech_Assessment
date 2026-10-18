using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class RentalApplicationViewModel
{
    public int ApplicationId { get; set; }
    public RentalApplicationStatus Status { get; set; }
    public bool ApplicantInformationCompleted { get; set; }
    public bool ResidenceHistoryCompleted { get; set; }

    public RentalApplicationSection Section { get; set; }
    public bool IsReadOnly { get; set; }

    public ApplicantInformationViewModel ApplicantInformation { get; set; } = new();

    public List<ResidenceHistoryEntryViewModel> ResidenceHistory { get; set; } = [];
}
