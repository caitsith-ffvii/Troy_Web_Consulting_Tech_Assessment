
namespace PropertyManagement.Domain.Entities;

public class Property
{
    public int Id { get; set; }

    public int PropertyManagerId { get; set; }

    public PropertyManager PropertyManager { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
