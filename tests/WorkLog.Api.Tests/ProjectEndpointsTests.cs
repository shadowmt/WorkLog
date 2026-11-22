using System.Net;
using System.Net.Http.Json;
using WorkLog.Api.Contracts.Projects;

namespace WorkLog.Api.Tests;

public class ProjectEndpointsTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PostProject_ShouldReturnCreatedWithLocation_WhenRequestIsValid()
    {
        // Arrange
        using var client = factory.CreateClient();

        var request = new CreateProjectRequest(
            $"API Project Test {Guid.NewGuid():N}",
            "Project API integration test");

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/projects",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith(
            "/api/projects/",
            response.Headers.Location!.OriginalString);

        var result = await response.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task PostProject_ShouldReturnConflict_WhenNameAlreadyExists()
    {
        // Arrange
        using var client = factory.CreateClient();

        var name = $"Duplicate Project Test {Guid.NewGuid():N}";

        var firstRequest = new CreateProjectRequest(
            name,
            "First project");

        var secondRequest = new CreateProjectRequest(
            name.ToLowerInvariant(),
            "Duplicate project");

        var firstResponse = await client.PostAsJsonAsync(
            "/api/projects",
            firstRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act
        var secondResponse = await client.PostAsJsonAsync(
            "/api/projects",
            secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var error = await secondResponse.Content
            .ReadFromJsonAsync<DuplicateProjectNameErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal(
            "同じ名前のプロジェクトが既に存在します。",
            error.Message);
    }

    [Fact]
    public async Task GetProjects_ShouldReturnProjectsOrderedByName()
    {
        // Arrange
        using var client = factory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N");

        var projectC = new CreateProjectRequest(
            $"Project C {suffix}",
            "3番目");

        var projectA = new CreateProjectRequest(
            $"Project A {suffix}",
            "1番目");

        var projectB = new CreateProjectRequest(
            $"Project B {suffix}",
            "2番目");

        await client.PostAsJsonAsync("/api/projects", projectC);
        await client.PostAsJsonAsync("/api/projects", projectA);
        await client.PostAsJsonAsync("/api/projects", projectB);

        // Act
        var response = await client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var projects = await response.Content
            .ReadFromJsonAsync<List<ProjectResponse>>();

        Assert.NotNull(projects);

        var actual = projects
            .Where(project => project.Name.EndsWith(suffix))
            .ToList();

        Assert.Collection(
            actual,
            project => Assert.StartsWith($"Project A {suffix}", project.Name),
            project => Assert.StartsWith($"Project B {suffix}", project.Name),
            project => Assert.StartsWith($"Project C {suffix}", project.Name));
    }
}

public sealed record DuplicateProjectNameErrorResponse(
    string Message);
