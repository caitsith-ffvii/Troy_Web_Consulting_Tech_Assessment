
using PropertyManagement.Domain.Identity;

namespace PropertyManagement.Domain.Entities;

public class PropertyManager
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Property> Properties { get; set; } = new List<Property>();
}
