
namespace PropertyManagement.Domain.Entities;

public class ApplicationStatusHistory
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    public string ChangedByUserId { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public RentalApplicationStatus PreviousStatus { get; set; }

    public RentalApplicationStatus NewStatus { get; set; }

    public string? Comment { get; set; }
}
