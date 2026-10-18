using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.ViewModels.Properties;

public class PropertyListViewModel
{
    public List<PropertyRowViewModel> Properties { get; set; } = [];
}

public class PropertyRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ManagerEmail { get; set; } = string.Empty;

    // Decided on the server: only the assigned manager may edit.
    public bool CanEdit { get; set; }

    public List<UnitRowViewModel> Units { get; set; } = [];
}

public class UnitRowViewModel
{
    public int Id { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public bool UnitTypeIsActive { get; set; }
}

public class PropertyFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;
}

public class UnitFormViewModel
{
    public int? Id { get; set; }

    public int PropertyId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Unit number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    [DataType(DataType.Currency)]
    [Display(Name = "Monthly rent")]
    public decimal MonthlyRent { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a unit type.")]
    [Display(Name = "Unit type")]
    public int UnitTypeId { get; set; }

    // The unit's saved type, which stays selectable even if inactive.
    public int? CurrentUnitTypeId { get; set; }
}

public class RemoveConfirmViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}
