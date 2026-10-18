using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Web.Services;

// Application lists for applicants (their own) and property managers (all).
// Scope, filters, count, sort, and paging all run in SQL Server.
public class ApplicationListService(ApplicationDbContext context)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public Task<PagedResult<RentalApplication>> GetApplicantApplicationsAsync(
        string applicantUserId,
        ApplicationListQuery query)
    {
        var scoped = context.RentalApplications
            .Where(a => a.Applicant.UserId == applicantUserId);

        return QueryAsync(scoped, query);
    }

    public async Task<PagedResult<RentalApplication>> GetManagerApplicationsAsync(
        string managerUserId,
        ApplicationListQuery query)
    {
        var isManager = await context.PropertyManagers
            .AnyAsync(m => m.UserId == managerUserId);

        // Every property manager sees every application.
        var scoped = isManager
            ? context.RentalApplications
            : context.RentalApplications.Where(_ => false);

        return await QueryAsync(scoped, query);
    }

    public async Task<List<Property>> GetApplicantPropertyOptionsAsync(
        string applicantUserId)
    {
        return await context.Properties
            .AsNoTracking()
            .Where(p => context.RentalApplications.Any(a =>
                a.Unit.PropertyId == p.Id &&
                a.Applicant.UserId == applicantUserId))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    // Archived properties are included so their history can still be filtered.
    public async Task<List<Property>> GetManagerPropertyOptionsAsync()
    {
        return await context.Properties
            .AsNoTracking()
            .OrderBy(p => p.IsArchived)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    private static async Task<PagedResult<RentalApplication>> QueryAsync(
        IQueryable<RentalApplication> scoped,
        ApplicationListQuery query)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var filtered = scoped.AsNoTracking();

        if (query.Status is { } status)
        {
            filtered = filtered.Where(a => a.Status == status);
        }

        if (query.PropertyId is { } propertyId)
        {
            filtered = filtered.Where(a => a.Unit.PropertyId == propertyId);
        }

        var totalCount = await filtered.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var pageNumber = Math.Clamp(query.PageNumber, 1, totalPages);

        var items = await Sort(filtered, query.Sort, query.Descending)
            .Include(a => a.ApplicantInformation)
            .Include(a => a.Unit)
                .ThenInclude(u => u.Property)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<RentalApplication>(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }

    // Only whitelisted fields; Id is the deterministic tie-breaker.
    private static IOrderedQueryable<RentalApplication> Sort(
        IQueryable<RentalApplication> applications,
        ApplicationSortField sort,
        bool descending)
    {
        var ordered = (sort, descending) switch
        {
            (ApplicationSortField.Applicant, false) => applications
                .OrderBy(a => a.ApplicantInformation.LastName)
                .ThenBy(a => a.ApplicantInformation.FirstName),
            (ApplicationSortField.Applicant, true) => applications
                .OrderByDescending(a => a.ApplicantInformation.LastName)
                .ThenByDescending(a => a.ApplicantInformation.FirstName),

            (ApplicationSortField.Property, false) => applications
                .OrderBy(a => a.Unit.Property.Name)
                .ThenBy(a => a.Unit.UnitNumber),
            (ApplicationSortField.Property, true) => applications
                .OrderByDescending(a => a.Unit.Property.Name)
                .ThenByDescending(a => a.Unit.UnitNumber),

            (ApplicationSortField.Status, false) => applications
                .OrderBy(a => a.Status),
            (ApplicationSortField.Status, true) => applications
                .OrderByDescending(a => a.Status),

            (_, false) => applications.OrderBy(a => a.SubmittedAtUtc),
            (_, true) => applications.OrderByDescending(a => a.SubmittedAtUtc)
        };

        return ordered.ThenBy(a => a.Id);
    }
}

public enum ApplicationSortField
{
    Submitted,
    Applicant,
    Property,
    Status
}

public sealed record ApplicationListQuery(
    RentalApplicationStatus? Status,
    int? PropertyId,
    ApplicationSortField Sort,
    bool Descending,
    int PageNumber,
    int PageSize = ApplicationListService.DefaultPageSize);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages =>
        Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
