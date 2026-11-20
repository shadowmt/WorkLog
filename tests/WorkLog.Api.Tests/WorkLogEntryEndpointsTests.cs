using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WorkLog.Api.Contracts.WorkLogEntries;
using WorkLog.Api.Contracts.Projects;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Api.Tests;

public class WorkLogEntryEndpointsTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PostWorkLogEntry_ShouldReturnCreated_WhenProjectIsActive()
    {
        // Arrange
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"API Test Project {Guid.NewGuid():N}",
            "API integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var request = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "API integration test work",
            60);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.WorkDate, result.WorkDate);
        Assert.Equal(request.ProjectId, result.ProjectId);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.DurationMinutes, result.DurationMinutes);
    }

    [Fact]
    public async Task PostWorkLogEntry_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        using var client = factory.CreateClient();

        var request = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            Guid.NewGuid(),
            "Project not found test",
            60);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content
            .ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.NotNull(error);
        Assert.True(error.ContainsKey("message"));
        Assert.Contains("Projectが見つかりません", error["message"]);
    }

    [Fact]
    public async Task PostWorkLogEntry_ShouldReturnUnprocessableEntity_WhenProjectIsInactive()
    {
        using var client = factory.CreateClient();

        // Arrange: API経由でProjectを作成
        var projectRequest = new CreateProjectRequest(
            $"Inactive Project Test {Guid.NewGuid():N}",
            "Inactive project test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        // テスト用DBでProjectを無効化
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<WorkLogDbContext>();

            var targetProject = await dbContext.Projects
                .FindAsync(project.Id);

            Assert.NotNull(targetProject);

            targetProject.Deactivate();

            await dbContext.SaveChangesAsync();
        }

        // Act: 無効化されたProjectに作業実績を登録
        var request = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "Inactive project work",
            60);

        var response = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        var error = await response.Content
            .ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.NotNull(error);
        Assert.True(error.ContainsKey("message"));
        Assert.Contains("無効なProjectには作業実績を登録できません", error["message"]);
    }

    [Fact]
    public async Task PostWorkLogEntry_ShouldReturnBadRequest_WhenDurationMinutesIsZero()
    {
        // Arrange
        using var client = factory.CreateClient();

        var request = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            Guid.NewGuid(),
            "Invalid duration test",
            0);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
