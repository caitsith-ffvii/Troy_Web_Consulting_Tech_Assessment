using Microsoft.AspNetCore.Identity;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Data;

namespace PropertyManagement.Web.Services;

public class RegistrationService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager)
{
    public async Task<RegistrationResult> RegisterAsync(
        RegistrationRequest request)
    {
        var errors = Validate(request);

        if (errors.Count > 0)
        {
            return RegistrationResult.Failure(errors);
        }

        // Account, role, and profile succeed or fail together; disposing an
        // uncommitted transaction rolls it back.
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
        {
            return RegistrationResult.Failure(ToErrors(created));
        }

        var roleAdded = await userManager.AddToRoleAsync(user, request.Role);

        if (!roleAdded.Succeeded)
        {
            return RegistrationResult.Failure(ToErrors(roleAdded));
        }

        if (request.Role == Roles.Applicant)
        {
            context.Applicants.Add(new Applicant
            {
                UserId = user.Id,
                FirstName = request.FirstName!.Trim(),
                LastName = request.LastName!.Trim(),
                PhoneNumber = request.PhoneNumber!.Trim(),
                CurrentAddress = request.CurrentAddress!.Trim()
            });
        }
        else
        {
            context.PropertyManagers.Add(new PropertyManager
            {
                UserId = user.Id
            });
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RegistrationResult.Success(user);
    }

    private static List<RegistrationError> Validate(
        RegistrationRequest request)
    {
        var errors = new List<RegistrationError>();

        if (request.Role is not (Roles.Applicant or Roles.PropertyManager))
        {
            errors.Add(new RegistrationError(
                "Role",
                "Select Applicant or Property Manager."));

            return errors;
        }

        if (request.Role == Roles.Applicant)
        {
            AddIfBlank(errors, request.FirstName, "FirstName", "First name is required.");
            AddIfBlank(errors, request.LastName, "LastName", "Last name is required.");
            AddIfBlank(errors, request.PhoneNumber, "PhoneNumber", "Phone is required.");
            AddIfBlank(errors, request.CurrentAddress, "CurrentAddress", "Current address is required.");
        }

        return errors;
    }

    private static void AddIfBlank(
        List<RegistrationError> errors,
        string? value,
        string field,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new RegistrationError(field, message));
        }
    }

    // Map Identity error codes onto the form fields they belong to.
    private static List<RegistrationError> ToErrors(IdentityResult result) =>
        result.Errors
            .Select(error => new RegistrationError(
                error.Code.StartsWith("Password")
                    ? "Password"
                    : error.Code is "DuplicateUserName"
                        or "DuplicateEmail"
                        or "InvalidEmail"
                        or "InvalidUserName"
                        ? "Email"
                        : null,
                error.Description))
            .ToList();
}

public sealed record RegistrationRequest(
    string Email,
    string Password,
    string Role,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? CurrentAddress);

public sealed record RegistrationError(
    string? Field,
    string Message);

public sealed record RegistrationResult(
    bool Succeeded,
    ApplicationUser? User,
    IReadOnlyList<RegistrationError> Errors)
{
    public static RegistrationResult Success(ApplicationUser user) =>
        new(true, user, []);

    public static RegistrationResult Failure(
        IReadOnlyList<RegistrationError> errors) =>
        new(false, null, errors);
}
