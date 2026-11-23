using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WorkLog.Api.Contracts.Projects;
using WorkLog.Api.Contracts.WorkLogEntries;
using WorkLog.Domain.Entities;
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

            targetProject.Deactivate(new DateTimeOffset(2026, 11, 22, 1, 0, 0, TimeSpan.Zero));

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

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnPagedResults_WhenRequestIsValid()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Projectを作成
        var projectRequest = new CreateProjectRequest(
            $"GET API Test Project {Guid.NewGuid():N}",
            "GET API integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        // 作業実績を登録
        var workLogRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "GET API integration test work",
            90);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            workLogRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Act
        var response = await client.GetAsync(
            $"/api/work-log-entries" +
            $"?from=2026-11-20" +
            $"&to=2026-11-20" +
            $"&projectId={project.Id}" +
            $"&page=1" +
            $"&pageSize=50");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);

        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);

        var item = Assert.Single(result.Items);

        Assert.Equal(workLogRequest.WorkDate, item.WorkDate);
        Assert.Equal(project.Id, item.ProjectId);
        Assert.Equal(project.Name, item.ProjectName);
        Assert.True(item.ProjectIsActive);
        Assert.Equal(workLogRequest.Description, item.Description);
        Assert.Equal(workLogRequest.DurationMinutes, item.DurationMinutes);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnCurrentMonthEntries_WhenDatesAreOmitted()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"Current Month Test Project {Guid.NewGuid():N}",
            "Current month integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var jstNow = factory.FakeTimeProvider.GetUtcNow().ToOffset(
            TimeSpan.FromHours(9));

        var today = DateOnly.FromDateTime(jstNow.DateTime);

        var currentMonthStart = new DateOnly(
            today.Year,
            today.Month,
            1);

        var previousMonthLastDay = currentMonthStart.AddDays(-1);

        var previousMonthEntry = new CreateWorkLogEntryRequest(
            previousMonthLastDay,
            project.Id,
            "Previous month work",
            60);

        var currentMonthEntry = new CreateWorkLogEntryRequest(
            currentMonthStart,
            project.Id,
            "Current month work",
            90);

        foreach (var request in new[]
        {
        previousMonthEntry,
        currentMonthEntry
    })
        {
            var createResponse = await client.PostAsJsonAsync(
                "/api/work-log-entries",
                request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var response = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);

        var item = Assert.Single(result.Items);

        Assert.Equal(currentMonthStart, item.WorkDate);
        Assert.Equal("Current month work", item.Description);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldFilterByFromDateOnly()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"From Date Test Project {Guid.NewGuid():N}",
            "From date integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var startDate = today.AddDays(-10);

        var dates = new[]
        {
        startDate.AddDays(-1),
        startDate,
        startDate.AddDays(1)
    };

        foreach (var date in dates)
        {
            var request = new CreateWorkLogEntryRequest(
                date,
                project.Id,
                $"Work on {date}",
                60);

            var createResponse = await client.PostAsJsonAsync(
                "/api/work-log-entries",
                request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var response = await client.GetAsync(
            $"/api/work-log-entries?from={startDate:yyyy-MM-dd}" +
            $"&projectId={project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        Assert.Equal(
            new[] { startDate.AddDays(1), startDate },
            result.Items.Select(x => x.WorkDate).ToArray());
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldFilterByToDateOnly()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"To Date Test Project {Guid.NewGuid():N}",
            "To date integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var endDate = today.AddDays(-10);

        var dates = new[]
        {
        endDate.AddDays(-1),
        endDate,
        endDate.AddDays(1)
    };

        foreach (var date in dates)
        {
            var request = new CreateWorkLogEntryRequest(
                date,
                project.Id,
                $"Work on {date}",
                60);

            var createResponse = await client.PostAsJsonAsync(
                "/api/work-log-entries",
                request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var response = await client.GetAsync(
            $"/api/work-log-entries?to={endDate:yyyy-MM-dd}" +
            $"&projectId={project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        Assert.Equal(
            new[] { endDate, endDate.AddDays(-1) },
            result.Items.Select(x => x.WorkDate).ToArray());
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnBadRequest_WhenFromIsAfterTo()
    {
        using var client = factory.CreateClient();

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var from = today;
        var to = today.AddDays(-1);

        var response = await client.GetAsync(
            $"/api/work-log-entries?from={from:yyyy-MM-dd}" +
            $"&to={to:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnBadRequest_WhenPageIsLessThanOne()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/work-log-entries?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnBadRequest_WhenPageSizeIsZero()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/work-log-entries?pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnBadRequest_WhenPageSizeExceedsMaximum()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/work-log-entries?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        using var client = factory.CreateClient();

        var nonExistentProjectId = Guid.NewGuid();

        // 指定したIDのProjectが存在しないことを確認する。
        var projectResponse = await client.GetAsync(
            $"/api/projects/{nonExistentProjectId}");

        Assert.Equal(HttpStatusCode.NotFound, projectResponse.StatusCode);

        // 存在しないProjectを指定した検索も404になることを確認する。
        var response = await client.GetAsync(
            $"/api/work-log-entries?projectId={nonExistentProjectId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnEmptyResult_WhenProjectHasNoEntries()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"Empty WorkLog Project {Guid.NewGuid():N}",
            "Project without work log entries");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var response = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnRequestedPage()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"Pagination Test Project {Guid.NewGuid():N}",
            "Pagination integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var dates = new[]
        {
        today.AddDays(-2),
        today.AddDays(-1),
        today
    };

        foreach (var date in dates)
        {
            var request = new CreateWorkLogEntryRequest(
                date,
                project.Id,
                $"Work on {date}",
                60);

            var createResponse = await client.PostAsJsonAsync(
                "/api/work-log-entries",
                request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        // 1ページ目：新しい日付から2件
        var firstPageResponse = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}&page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);

        var firstPage = await firstPageResponse.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(firstPage);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(2, firstPage.PageSize);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);

        Assert.Equal(
            new[] { today, today.AddDays(-1) },
            firstPage.Items.Select(x => x.WorkDate).ToArray());

        // 2ページ目：残り1件
        var secondPageResponse = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}&page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, secondPageResponse.StatusCode);

        var secondPage = await secondPageResponse.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(secondPage);
        Assert.Single(secondPage.Items);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(2, secondPage.PageSize);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(2, secondPage.TotalPages);

        Assert.Equal(today.AddDays(-2), secondPage.Items[0].WorkDate);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnEmptyItems_WhenPageExceedsTotalPages()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"Beyond Page Test Project {Guid.NewGuid():N}",
            "Page beyond total pages integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var request = new CreateWorkLogEntryRequest(
            today,
            project.Id,
            "Single work log entry",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}&page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnProjectInformation()
    {
        using var client = factory.CreateClient();

        var projectName = $"Project Information Test {Guid.NewGuid():N}";

        var projectRequest = new CreateProjectRequest(
            projectName,
            "Project information integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var workLogRequest = new CreateWorkLogEntryRequest(
            today,
            project.Id,
            "Project information test entry",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            workLogRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);

        var item = Assert.Single(result.Items);

        Assert.Equal(project.Id, item.ProjectId);
        Assert.Equal(projectName, item.ProjectName);
        Assert.True(item.ProjectIsActive);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldReturnStableOrdering()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"Stable Ordering Test {Guid.NewGuid():N}",
            "Stable ordering integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var dates = new[]
        {
            today.AddDays(-2),
            today,
            today.AddDays(-1)
        };

        foreach (var date in dates)
        {
            var request = new CreateWorkLogEntryRequest(
                date,
                project.Id,
                $"Work on {date}",
                60);

            var createResponse = await client.PostAsJsonAsync(
                "/api/work-log-entries",
                request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var url = $"/api/work-log-entries?projectId={project.Id}";

        var firstResponse = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstResult = await firstResponse.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(firstResult);

        var secondResponse = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondResult = await secondResponse.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(secondResult);

        Assert.Equal(
            new[]
            {
                today,
                today.AddDays(-1),
                today.AddDays(-2)
            },
            firstResult.Items.Select(x => x.WorkDate).ToArray());

        Assert.Equal(
            firstResult.Items.Select(x => x.Id),
            secondResult.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldUseDefaultPaginationValues()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"Default Pagination Test {Guid.NewGuid():N}",
            "Default pagination integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var today = DateOnly.FromDateTime(
            factory.FakeTimeProvider.GetUtcNow()
                .ToOffset(TimeSpan.FromHours(9))
                .DateTime);

        var request = new CreateWorkLogEntryRequest(
            today,
            project.Id,
            "Default pagination test entry",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetWorkLogEntries_ShouldAcceptMaximumPageSize()
    {
        using var client = factory.CreateClient();

        var projectRequest = new CreateProjectRequest(
            $"Maximum Page Size Test {Guid.NewGuid():N}",
            "Maximum page size integration test");

        var projectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            projectRequest);

        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var project = await projectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);

        var response = await client.GetAsync(
            $"/api/work-log-entries?projectId={project.Id}&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedWorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(1, result.Page);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldUpdateDescription_WhenDescriptionPropertyNameIsPascalCase()
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

        var createRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "変更前の作業記録",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdEntry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(createdEntry);

        const string newDescription = "変更後の作業記録";

        // Act
        using var content = new StringContent(
            """{"Description":"変更後の作業記録"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PatchAsync(
            $"/api/work-log-entries/{createdEntry.Id}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(createdEntry.Id, result.Id);
        Assert.Equal(createdEntry.WorkDate, result.WorkDate);
        Assert.Equal(createdEntry.ProjectId, result.ProjectId);
        Assert.Equal(newDescription, result.Description);
        Assert.Equal(createdEntry.DurationMinutes, result.DurationMinutes);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldReturnNotFound_WhenEntryDoesNotExist()
    {
        // Arrange
        using var client = factory.CreateClient();

        var entryId = Guid.NewGuid();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/work-log-entries/{entryId}",
            new { description = "更新後の説明" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldReturnNotFound_WhenTargetProjectDoesNotExist()
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

        var createRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "変更前の作業記録",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdEntry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(createdEntry);

        var missingProjectId = Guid.NewGuid();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/work-log-entries/{createdEntry.Id}",
            new { projectId = missingProjectId });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldAllowInactiveProject_WhenProjectIdIsSpecified()
    {
        // Arrange
        using var client = factory.CreateClient();

        // 変更前のProjectを作成
        var originalProjectRequest = new CreateProjectRequest(
            $"Original Project {Guid.NewGuid():N}",
            "Original project");

        var originalProjectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            originalProjectRequest);

        Assert.Equal(HttpStatusCode.Created, originalProjectResponse.StatusCode);

        var originalProject = await originalProjectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(originalProject);

        // 変更先のProjectを作成
        var targetProjectRequest = new CreateProjectRequest(
            $"Target Project {Guid.NewGuid():N}",
            "Target project");

        var targetProjectResponse = await client.PostAsJsonAsync(
            "/api/projects",
            targetProjectRequest);

        Assert.Equal(HttpStatusCode.Created, targetProjectResponse.StatusCode);

        var targetProject = await targetProjectResponse.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(targetProject);

        // WorkLogEntryを作成
        var createRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            originalProject.Id,
            "既存の作業記録",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdEntry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(createdEntry);

        // テスト用DbContextから変更先Projectを無効化
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<WorkLogDbContext>();

            var project = await dbContext.Set<Project>()
                .SingleAsync(x => x.Id == targetProject.Id);

            project.Deactivate(factory.FakeTimeProvider.GetUtcNow());

            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/work-log-entries/{createdEntry.Id}",
            new { projectId = targetProject.Id });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(targetProject.Id, result.ProjectId);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldReturnBadRequest_WhenNoFieldsAreSpecified()
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

        var createRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "API integration test work",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var entry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(entry);

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/work-log-entries/{entry.Id}",
            new { });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldReturnBadRequest_WhenDescriptionIsNull()
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

        var createRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "API integration test work",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var entry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(entry);

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/work-log-entries/{entry.Id}",
            new { description = (string?)null });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldUpdateWorkDate_WhenWorkDateIsSpecified()
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

        var originalWorkDate = new DateOnly(2026, 11, 20);
        var updatedWorkDate = new DateOnly(2026, 11, 19);

        var createRequest = new CreateWorkLogEntryRequest(
            originalWorkDate,
            project.Id,
            "API integration test work",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var entry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(entry);

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/work-log-entries/{entry.Id}",
            new { workDate = updatedWorkDate });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(updatedWorkDate, result.WorkDate);
        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(createRequest.Description, result.Description);
        Assert.Equal(createRequest.DurationMinutes, result.DurationMinutes);
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldUpdateDurationMinutes_WhenDurationMinutesIsSpecified()
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

        var createRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "API integration test work",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var entry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(entry);

        const int updatedDurationMinutes = 120;

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/work-log-entries/{entry.Id}",
            new { durationMinutes = updatedDurationMinutes });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(result);
        Assert.Equal(createRequest.WorkDate, result.WorkDate);
        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(createRequest.Description, result.Description);
        Assert.Equal(updatedDurationMinutes, result.DurationMinutes);
    }

    [Fact]
    public async Task DeleteWorkLogEntry_ShouldReturnNoContent_WhenEntryExists()
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

        var createRequest = new CreateWorkLogEntryRequest(
            new DateOnly(2026, 11, 20),
            project.Id,
            "削除対象の作業記録",
            60);

        var createResponse = await client.PostAsJsonAsync(
            "/api/work-log-entries",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdEntry = await createResponse.Content
            .ReadFromJsonAsync<WorkLogEntryResponse>();

        Assert.NotNull(createdEntry);

        // 削除前に一覧へ現れることを確認しておく（削除後の不在検証を空虚にしないため）。
        // テスト DB は実行をまたいで行が残るため、プロジェクトで絞り込んで自テストの行だけを見る。
        var listUrl =
            $"/api/work-log-entries?from=2026-11-20&to=2026-11-20&projectId={project.Id}";

        Assert.Contains(createdEntry.Id, await GetWorkLogEntryIdsAsync(client, listUrl));

        // Act
        var response = await client.DeleteAsync(
            $"/api/work-log-entries/{createdEntry.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // 削除後に一覧から取得できないことを確認
        Assert.DoesNotContain(createdEntry.Id, await GetWorkLogEntryIdsAsync(client, listUrl));

        static async Task<ICollection<Guid>> GetWorkLogEntryIdsAsync(
            HttpClient httpClient,
            string requestUri)
        {
            var listResponse = await httpClient.GetAsync(requestUri);

            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            using var json = await JsonDocument.ParseAsync(
                await listResponse.Content.ReadAsStreamAsync());

            return json.RootElement.GetProperty("items")
                .EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid())
                .ToList();
        }
    }

    [Fact]
    public async Task DeleteWorkLogEntry_ShouldReturnNotFound_WhenEntryDoesNotExist()
    {
        // Arrange
        using var client = factory.CreateClient();

        var entryId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync(
            $"/api/work-log-entries/{entryId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
