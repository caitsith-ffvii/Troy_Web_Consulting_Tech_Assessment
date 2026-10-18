using System.Net;
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
