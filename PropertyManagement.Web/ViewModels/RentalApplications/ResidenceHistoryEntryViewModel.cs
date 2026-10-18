
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ResidenceHistoryEntryViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Display(Name = "Landlord name")]
    public string LandlordName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    [Display(Name = "Landlord phone")]
    public string LandlordPhoneNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Move-in date")]
    public DateTime MoveInDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Move-out date")]
    public DateTime? MoveOutDate { get; set; }

    // Field-level feedback for the modal; RentalApplicationService enforces
    // the same rules.
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (MoveInDate.Date > DateTime.Today)
        {
            yield return new ValidationResult(
                "Move-in date cannot be in the future.",
                [nameof(MoveInDate)]);
        }

        if (MoveOutDate.HasValue && MoveOutDate.Value.Date < MoveInDate.Date)
        {
            yield return new ValidationResult(
                "Move-out date cannot be before move-in date.",
                [nameof(MoveOutDate)]);
        }
    }
}

public class ResidenceHistoryListViewModel
{
    public int ApplicationId { get; set; }

    public bool IsReadOnly { get; set; }

    public List<ResidenceHistoryEntryViewModel> Entries { get; set; } = [];
}
