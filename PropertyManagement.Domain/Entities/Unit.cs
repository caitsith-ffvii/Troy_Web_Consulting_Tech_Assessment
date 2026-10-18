
namespace PropertyManagement.Domain.Entities;

public class Unit
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public int UnitTypeId { get; set; }
    public UnitType UnitType { get; set; } = null!;
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public bool IsArchived { get; set; }
}
