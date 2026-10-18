using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

/// <summary>
/// Pages of rental applications for the application grid. Filtering,
/// sorting, counting, and paging all run in the database.
/// </summary>
[ApiController]
[Route("api/applications")]
[Produces("application/json")]
public class ApplicationsApiController(ApplicationListService listService)
    : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// All applications, for property managers.
    /// </summary>
    /// <remarks>
    /// Defaults: status Submitted, oldest submission first, 20 per page.
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = Roles.PropertyManager)]
    [ProducesResponseType<ApplicationGridPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ApplicationGridPage> GetAll(
        [FromQuery] ApplicationGridRequest request)
    {
        var query = request.ToQuery(
            defaultStatus: RentalApplicationStatus.Submitted,
            defaultDescending: false);

        var result = await listService.GetManagerApplicationsAsync(
            CurrentUserId,
            query);

        return ToPage(result, query, "ManageApplications");
    }

    /// <summary>
    /// The signed-in applicant's own applications.
    /// </summary>
    /// <remarks>
    /// Defaults: all statuses, most recent submission first, 20 per page.
    /// </remarks>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Applicant)]
    [ProducesResponseType<ApplicationGridPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ApplicationGridPage> GetMine(
        [FromQuery] ApplicationGridRequest request)
    {
        var query = request.ToQuery(
            defaultStatus: null,
            defaultDescending: true);

        var result = await listService.GetApplicantApplicationsAsync(
            CurrentUserId,
            query);

        return ToPage(result, query, "Applications");
    }

    // Echoes the applied filters so role defaults live only on the server.
    private ApplicationGridPage ToPage(
        PagedResult<RentalApplication> result,
        ApplicationListQuery query,
        string detailsController) =>
        new(
            result.Items
                .Select(a => new ApplicationGridRow(
                    a.Id,
                    $"{a.ApplicantInformation.FirstName} {a.ApplicantInformation.LastName}",
                    a.Unit.Property.Name,
                    a.Unit.UnitNumber,
                    a.Status,
                    // SQL Server returns an unspecified kind; mark it UTC so
                    // the JSON carries a "Z" and browsers convert to local time.
                    a.SubmittedAtUtc is { } submitted
                        ? DateTime.SpecifyKind(submitted, DateTimeKind.Utc)
                        : null,
                    Url.Action("Details", detailsController, new { id = a.Id })!))
                .ToList(),
            result.TotalCount,
            result.PageNumber,
            result.PageSize,
            result.TotalPages,
            ApplicationStatusFilter.Format(query.Status),
            query.PropertyId,
            query.Sort,
            query.Descending);
}

/// <summary>Query parameters for an application grid page.</summary>
public sealed record ApplicationGridRequest
{
    /// <summary>"All" or a status name. Omit for the endpoint's default.</summary>
    public string? Status { get; init; }

    /// <summary>Only applications for this property.</summary>
    public int? PropertyId { get; init; }

    /// <summary>Sort field: Submitted (default), Applicant, Property, or Status.</summary>
    public ApplicationSortField? Sort { get; init; }

    /// <summary>Sort descending. Omit for the endpoint's default.</summary>
    public bool? Desc { get; init; }

    /// <summary>1-based page number; past the last page returns the last page.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Rows per page, 1–100.</summary>
    public int PageSize { get; init; } = ApplicationListService.DefaultPageSize;

    internal ApplicationListQuery ToQuery(
        RentalApplicationStatus? defaultStatus,
        bool defaultDescending)
    {
        // Explicit whitelist: an undefined numeric enum value falls back.
        var sort = Sort is { } requested && Enum.IsDefined(requested)
            ? requested
            : ApplicationSortField.Submitted;

        return new ApplicationListQuery(
            ApplicationStatusFilter.Parse(Status, defaultStatus),
            PropertyId,
            sort,
            Desc ?? defaultDescending,
            Page,
            PageSize);
    }
}

/// <summary>One page of applications and the filtered total.</summary>
/// <param name="Items">The rows on this page.</param>
/// <param name="TotalCount">Applications matching the filters across all pages.</param>
/// <param name="PageNumber">The page returned (clamped to the last page).</param>
/// <param name="PageSize">Rows per page.</param>
/// <param name="TotalPages">Total pages for the filtered set.</param>
/// <param name="Status">The applied status filter ("All" or a status name).</param>
/// <param name="PropertyId">The applied property filter.</param>
/// <param name="Sort">The applied sort field.</param>
/// <param name="Descending">Whether the sort is descending.</param>
public sealed record ApplicationGridPage(
    IReadOnlyList<ApplicationGridRow> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    string Status,
    int? PropertyId,
    ApplicationSortField Sort,
    bool Descending);

/// <summary>One application in the grid.</summary>
/// <param name="Id">Application id.</param>
/// <param name="ApplicantName">Name from the application's snapshot.</param>
/// <param name="PropertyName">Property name.</param>
/// <param name="UnitNumber">Unit number.</param>
/// <param name="Status">Current status.</param>
/// <param name="SubmittedAtUtc">Most recent submission time; null for drafts.</param>
/// <param name="DetailsUrl">Page for viewing the application.</param>
public sealed record ApplicationGridRow(
    int Id,
    string ApplicantName,
    string PropertyName,
    string UnitNumber,
    RentalApplicationStatus Status,
    DateTime? SubmittedAtUtc,
    string DetailsUrl);
