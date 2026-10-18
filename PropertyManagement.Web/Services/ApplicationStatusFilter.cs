using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Web.Services;

// The status filter as it appears in query strings: "All" or a status name.
public static class ApplicationStatusFilter
{
    public const string All = "All";

    // Anything unrecognized, including numeric values, uses the default.
    public static RentalApplicationStatus? Parse(
        string? status,
        RentalApplicationStatus? defaultStatus)
    {
        if (string.Equals(status, All, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Enum.TryParse<RentalApplicationStatus>(status, ignoreCase: true, out var parsed) &&
               Enum.IsDefined(parsed) &&
               !int.TryParse(status, out _)
            ? parsed
            : defaultStatus;
    }

    public static string Format(RentalApplicationStatus? status) =>
        status?.ToString() ?? All;
}
