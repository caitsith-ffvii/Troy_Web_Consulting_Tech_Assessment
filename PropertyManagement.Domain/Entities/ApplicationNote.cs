namespace PropertyManagement.Domain.Entities;

// An internal note on an application, for property managers only.
// RentalApplication deliberately has no Notes collection, so applicant-side
// queries cannot include notes.
public class ApplicationNote
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    public string AuthorUserId { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
