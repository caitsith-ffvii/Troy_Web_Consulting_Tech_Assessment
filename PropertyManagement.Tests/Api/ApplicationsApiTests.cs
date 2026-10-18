using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Tests.Infrastructure;

namespace PropertyManagement.Tests.Api;

// HTTP tests for the grid's JSON endpoint. Paging and sorting logic is
// covered by ApplicationListServiceTests; these cover the contract and
// authorization. The shared test database holds other data, so results
// are scoped to each test's own property or applicant.
public class ApplicationsApiTests(WebAppFactory factory)
    : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task GetAll_Anonymous_Returns401InsteadOfRedirect()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/api/applications");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Applicant_Returns403()
    {
        // Arrange
        var client = factory.CreateClientAs("any-user", Roles.Applicant);

        // Act
        var response = await client.GetAsync("/api/applications");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ManagerFilteredToProperty_ReturnsPageAndFilteredTotal()
    {
        // Arrange
        var scenario = await CreateScenarioAsync();
        var client = factory.CreateClientAs(
            scenario.ManagerUser.Id,
            Roles.PropertyManager);

        // Act
        var response = await client.GetAsync(
            $"/api/applications?status=Submitted&propertyId={scenario.Unit.PropertyId}&pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, root.GetProperty("pageNumber").GetInt32());
        Assert.Equal(5, root.GetProperty("pageSize").GetInt32());
        Assert.Equal("Submitted", root.GetProperty("status").GetString());
        Assert.Equal("Submitted", root.GetProperty("sort").GetString());

        var row = Assert.Single(root.GetProperty("items").EnumerateArray());

        Assert.Equal(scenario.Application.Id, row.GetProperty("id").GetInt32());
        Assert.Equal("Submitted", row.GetProperty("status").GetString());
        Assert.Equal(
            $"/ManageApplications/Details/{scenario.Application.Id}",
            row.GetProperty("detailsUrl").GetString());
    }

    [Fact]
    public async Task GetAll_NoFilters_AppliesManagerDefaults()
    {
        // Arrange
        var scenario = await CreateScenarioAsync();
        var client = factory.CreateClientAs(
            scenario.ManagerUser.Id,
            Roles.PropertyManager);

        // Act
        var page = await client.GetFromJsonAsync<JsonElement>("/api/applications");

        // Assert
        Assert.Equal("Submitted", page.GetProperty("status").GetString());
        Assert.False(page.GetProperty("descending").GetBoolean());
        Assert.Equal(20, page.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task GetMine_Applicant_ReturnsOnlyOwnApplications()
    {
        // Arrange
        var mine = await CreateScenarioAsync();
        var other = await CreateScenarioAsync();
        var client = factory.CreateClientAs(
            mine.ApplicantUser.Id,
            Roles.Applicant);

        // Act
        var page = await client.GetFromJsonAsync<JsonElement>(
            "/api/applications/mine?pageSize=100");

        // Assert
        var ids = page.GetProperty("items")
            .EnumerateArray()
            .Select(row => row.GetProperty("id").GetInt32())
            .ToList();

        Assert.Contains(mine.Application.Id, ids);
        Assert.DoesNotContain(other.Application.Id, ids);
        Assert.Equal("All", page.GetProperty("status").GetString());
        Assert.True(page.GetProperty("descending").GetBoolean());
    }

    [Fact]
    public async Task OpenApiDocument_DescribesBothEndpoints()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var document = await client.GetStringAsync("/openapi/v1.json");

        // Assert
        Assert.Contains("\"/api/applications\"", document);
        Assert.Contains("\"/api/applications/mine\"", document);
        Assert.Contains("totalCount", document);
    }

    private static async Task<SubmittedApplicationScenario> CreateScenarioAsync()
    {
        var database = new SqlServerTestDatabase();
        await database.InitializeAsync();

        await using var context = database.CreateContext();
        var scenario = await SubmittedApplicationScenarioFactory.CreateAsync(context);

        scenario.Application.SubmittedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return scenario;
    }
}
