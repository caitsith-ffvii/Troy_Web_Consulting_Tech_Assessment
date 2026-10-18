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

    // The manager's comment explaining the current Returned or Denied status.
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    // Set by the controller (never bound from the form); picks the back link.
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public bool FromAvailableUnits { get; set; }

    // A read-only copy for rendering section partials inside the Summary,
    // which is always read-only even while the application is editable.
    public RentalApplicationViewModel AsReadOnly()
    {
        var copy = (RentalApplicationViewModel)MemberwiseClone();
        copy.IsReadOnly = true;
        return copy;
    }
}
