using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.ViewModels.RentalApplications;

public class ApplicationListViewModel
{
    public const string AllStatuses = "All";

    public List<ApplicationRowViewModel> Rows { get; set; } = [];

    // "All" or a status name; kept as text so "All" survives round trips.
    public string Status { get; set; } = AllStatuses;
    public int? PropertyId { get; set; }
    public ApplicationSortField Sort { get; set; }
    public bool Descending { get; set; }

    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public List<SelectListItem> StatusOptions { get; set; } = [];
    public List<SelectListItem> PropertyOptions { get; set; } = [];

    // Applicants only see their own applications, so the column is noise.
    public bool ShowApplicant { get; set; }

    public string ListController { get; set; } = string.Empty;
    public string DetailsController { get; set; } = string.Empty;

    // Parses the status filter; anything unrecognized uses the default.
    public static RentalApplicationStatus? ParseStatus(
        string? status,
        RentalApplicationStatus? defaultStatus) =>
        ApplicationStatusFilter.Parse(status, defaultStatus);

    public static ApplicationListViewModel Create(
        PagedResult<RentalApplication> result,
        RentalApplicationStatus? status,
        int? propertyId,
        ApplicationSortField sort,
        bool descending,
        IEnumerable<Property> propertyOptions,
        bool showApplicant,
        string listController,
        string detailsController)
    {
        return new ApplicationListViewModel
        {
            Rows = result.Items
                .Select(a => new ApplicationRowViewModel
                {
                    Id = a.Id,
                    ApplicantName =
                        $"{a.ApplicantInformation.FirstName} {a.ApplicantInformation.LastName}",
                    PropertyName = a.Unit.Property.Name,
                    UnitNumber = a.Unit.UnitNumber,
                    Status = a.Status,
                    SubmittedAtUtc = a.SubmittedAtUtc
                })
                .ToList(),
            Status = status?.ToString() ?? AllStatuses,
            PropertyId = propertyId,
            Sort = sort,
            Descending = descending,
            PageNumber = result.PageNumber,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            StatusOptions = Enum.GetValues<RentalApplicationStatus>()
                .Select(s => s.ToString())
                .Prepend(AllStatuses)
                .Select(s => new SelectListItem(s, s, s == (status?.ToString() ?? AllStatuses)))
                .ToList(),
            PropertyOptions = propertyOptions
                .Select(p => new SelectListItem(
                    p.IsArchived ? $"{p.Name} (archived)" : p.Name,
                    p.Id.ToString(),
                    p.Id == propertyId))
                .Prepend(new SelectListItem("All properties", "", propertyId is null))
                .ToList(),
            ShowApplicant = showApplicant,
            ListController = listController,
            DetailsController = detailsController
        };
    }
}

public class ApplicationRowViewModel
{
    public int Id { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public RentalApplicationStatus Status { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
}
