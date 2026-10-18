namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ResidenceHistoryEntryViewModel
{
    public string Address { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhoneNumber { get; set; } = string.Empty;
    public DateTime MoveInDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
}
