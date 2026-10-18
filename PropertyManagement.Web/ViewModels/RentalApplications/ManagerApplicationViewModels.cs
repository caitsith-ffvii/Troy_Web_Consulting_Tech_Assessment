using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ManagerApplicationDetailsViewModel
{
    public RentalApplicationViewModel Application { get; set; } = new();

    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public string AssignedManagerEmail { get; set; } = string.Empty;

    // Decided on the server: the assigned manager, on a Submitted application.
    public bool CanReview { get; set; }
}

public enum ReviewOutcome
{
    Approve,
    Return,
    Deny
}

public class ReviewFormViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "Choose an outcome.")]
    public ReviewOutcome? Outcome { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }

    // Shown so the manager knows what approval will create.
    public decimal MonthlyRent { get; set; }

    public RentalApplicationStatus? NewStatus => Outcome switch
    {
        ReviewOutcome.Approve => RentalApplicationStatus.Approved,
        ReviewOutcome.Return => RentalApplicationStatus.Returned,
        ReviewOutcome.Deny => RentalApplicationStatus.Denied,
        _ => null
    };

    // Field-level feedback; RentalApplicationService enforces the same rule.
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Outcome is ReviewOutcome.Return or ReviewOutcome.Deny &&
            string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult(
                "A comment is required when returning or denying an application.",
                [nameof(Comment)]);
        }
    }
}
