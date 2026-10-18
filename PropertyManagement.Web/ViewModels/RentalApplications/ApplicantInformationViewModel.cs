using System.ComponentModel.DataAnnotations;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ApplicantInformationViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(PhoneNumberFormat.Pattern, ErrorMessage = PhoneNumberFormat.ErrorMessage)]
    [StringLength(30)]
    [Display(Name = "Phone")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    [Display(Name = "Current address")]
    public string CurrentAddress { get; set; } = string.Empty;
}
