using System.ComponentModel.DataAnnotations;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ApplicantInformationViewModel
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(PhoneNumberFormat.Pattern, ErrorMessage = PhoneNumberFormat.ErrorMessage)]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string CurrentAddress { get; set; } = string.Empty;
}
