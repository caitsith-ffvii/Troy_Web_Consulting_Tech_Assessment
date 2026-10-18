
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ResidenceHistoryEntryViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string LandlordName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string LandlordPhoneNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime MoveInDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? MoveOutDate { get; set; }
}
