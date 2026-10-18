using System.ComponentModel.DataAnnotations;
using PropertyManagement.Web.Services;
using PropertyManagement.Domain.Identity;

namespace PropertyManagement.Web.ViewModels.Account;

public class RegisterViewModel : IValidatableObject
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Roles.Applicant;

    [StringLength(100)]
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    [RegularExpression(PhoneNumberFormat.Pattern, ErrorMessage = PhoneNumberFormat.ErrorMessage)]
    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    [StringLength(300)]
    [Display(Name = "Current address")]
    public string? CurrentAddress { get; set; }

    // Applicant-only fields; RegistrationService enforces the same rule.
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Role != Roles.Applicant)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult(
                "First name is required.",
                [nameof(FirstName)]);
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult(
                "Last name is required.",
                [nameof(LastName)]);
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            yield return new ValidationResult(
                "Phone is required.",
                [nameof(PhoneNumber)]);
        }

        if (string.IsNullOrWhiteSpace(CurrentAddress))
        {
            yield return new ValidationResult(
                "Current address is required.",
                [nameof(CurrentAddress)]);
        }
    }
}
