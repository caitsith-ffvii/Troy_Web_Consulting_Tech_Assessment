using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.ViewComponents;

public enum ApplicationGridScope
{
    Applicant,
    Manager
}

// A reusable application grid. It renders the shell and filter options;
// wwwroot/js/application-grid.js loads every page of rows from the JSON
// endpoint for the scope.
public class ApplicationGridViewComponent(ApplicationListService listService)
    : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(ApplicationGridScope scope)
    {
        var isManager = scope == ApplicationGridScope.Manager;

        var properties = isManager
            ? await listService.GetManagerPropertyOptionsAsync()
            : await listService.GetApplicantPropertyOptionsAsync(
                UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return View(new ApplicationGridViewModel(
            DataUrl: Url.Action(
                isManager ? "GetAll" : "GetMine",
                "ApplicationsApi")!,
            ShowApplicant: isManager,
            HighlightReturned: !isManager,
            StatusOptions: Enum.GetNames<RentalApplicationStatus>()
                .Prepend(ApplicationStatusFilter.All)
                .ToList(),
            PropertyOptions: properties
                .Select(p => new PropertyOption(
                    p.Id,
                    p.IsArchived ? $"{p.Name} (archived)" : p.Name))
                .ToList()));
    }
}

public sealed record ApplicationGridViewModel(
    string DataUrl,
    bool ShowApplicant,
    bool HighlightReturned,
    IReadOnlyList<string> StatusOptions,
    IReadOnlyList<PropertyOption> PropertyOptions);

public sealed record PropertyOption(int Id, string Label);
