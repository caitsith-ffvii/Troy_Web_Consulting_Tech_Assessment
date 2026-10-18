using System.Net;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Tests.Infrastructure;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Api;

// End-to-end checks that notes reach managers and never reach applicants,
// through rendered pages, the JSON API, and the notes endpoints.
public class ApplicationNotesHttpTests(WebAppFactory factory)
    : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task ApplicantApplicationPage_DoesNotRenderNotes()
    {
        // Arrange
        var (scenario, noteText) = await CreateScenarioWithNoteAsync();
        var client = factory.CreateClientAs(scenario.ApplicantUser.Id, Roles.Applicant);

        // Act
        var response = await client.GetAsync($"/Applications/Details/{scenario.Application.Id}");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(noteText, html);
    }

    [Fact]
    public async Task ApplicantJsonApi_DoesNotReturnNotes()
    {
        // Arrange
        var (scenario, noteText) = await CreateScenarioWithNoteAsync();
        var client = factory.CreateClientAs(scenario.ApplicantUser.Id, Roles.Applicant);

        // Act
        var json = await client.GetStringAsync("/api/applications/mine?pageSize=100");

        // Assert
        Assert.Contains($"\"id\":{scenario.Application.Id}", json);
        Assert.DoesNotContain(noteText, json);
    }

    [Fact]
    public async Task ApplicantNotesEndpoint_IsForbidden()
    {
        // Arrange
        var (scenario, _) = await CreateScenarioWithNoteAsync();
        var client = factory.CreateClientAs(scenario.ApplicantUser.Id, Roles.Applicant);

        // Act
        var response = await client.GetAsync($"/ManageApplications/Notes/{scenario.Application.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ManagerApplicationPage_RendersNotes()
    {
        // Arrange
        var (scenario, noteText) = await CreateScenarioWithNoteAsync();
        var client = factory.CreateClientAs(scenario.ManagerUser.Id, Roles.PropertyManager);

        // Act
        var html = await client.GetStringAsync(
            $"/ManageApplications/Details/{scenario.Application.Id}");

        // Assert
        Assert.Contains(noteText, html);
    }

    // Not about notes, but uses the same host: a post to a locked
    // (Submitted) application is rejected visibly and saves nothing.
    [Fact]
    public async Task Navigate_LockedApplication_Returns409WithoutSaving()
    {
        // Arrange
        var (scenario, _) = await CreateScenarioWithNoteAsync();
        var client = factory.CreateClientAs(scenario.ApplicantUser.Id, Roles.Applicant);

        // Any page with a form issues an anti-forgery token for this user.
        var formPage = await client.GetStringAsync("/AvailableUnits");
        var token = System.Text.RegularExpressions.Regex.Match(
            formPage,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        Assert.False(string.IsNullOrEmpty(token));

        // Act
        var response = await client.PostAsync(
            $"/Applications/Navigate/{scenario.Application.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["ApplicationId"] = scenario.Application.Id.ToString(),
                ["Section"] = "ApplicantInformation",
                ["command"] = "Continue",
                ["ApplicantInformation.FirstName"] = "Tampered",
                ["ApplicantInformation.LastName"] = "Applicant",
                ["ApplicantInformation.PhoneNumber"] = "(555) 555-0100",
                ["ApplicantInformation.Email"] = "tampered@example.com",
                ["ApplicantInformation.CurrentAddress"] = "1 Tampered Street"
            }));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(
            "This application can no longer be edited.",
            await response.Content.ReadAsStringAsync());

        var database = new SqlServerTestDatabase();
        await using var context = database.CreateContext();

        var snapshot = await context.ApplicantInformationSnapshots
            .SingleAsync(s => s.RentalApplicationId == scenario.Application.Id);

        Assert.Equal("Test", snapshot.FirstName);
    }

    private static async Task<(SubmittedApplicationScenario Scenario, string NoteText)>
        CreateScenarioWithNoteAsync()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        var scenario = await SubmittedApplicationScenarioFactory.CreateAsync(context);

        var noteText = $"Internal note {Guid.NewGuid():N}";

        var result = await new ApplicationNoteService(context).AddAsync(
            scenario.Application.Id,
            scenario.ManagerUser.Id,
            noteText);

        Assert.True(result.Succeeded);

        return (scenario, noteText);
    }
}
