namespace PropertyManagement.Domain.Entities;

public class Lease
{
    public int Id { get; set; }

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public int ApplicantId { get; set; }
    public Applicant Applicant { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal AgreedRent { get; set; }
}