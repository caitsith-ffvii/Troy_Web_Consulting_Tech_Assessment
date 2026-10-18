
namespace PropertyManagement.Domain.Entities;

public class Property
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public ICollection<PropertyManager> PropertyManagers { get; set; } = new List<PropertyManager>();
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
