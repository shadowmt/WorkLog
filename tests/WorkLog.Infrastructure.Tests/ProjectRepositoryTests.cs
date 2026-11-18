using WorkLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace WorkLog.Infrastructure.Tests;

public class ProjectRepositoryTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task CanCreateDbContext()
    {
        await using var dbContext = fixture.CreateDbContext();

        Assert.NotNull(dbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistProject()
    {
        var projectName = $"テストプロジェクト_{Guid.NewGuid():N}";

        var project = new Project(
            projectName,
            "永続化テスト");

        // Act
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            var actual = await dbContext.Projects
                .SingleAsync(x => x.Id == project.Id);

            Assert.Equal(project.Id, actual.Id);
            Assert.Equal(projectName, actual.Name);
            Assert.Equal(projectName.ToUpperInvariant(), actual.NormalizedName);
            Assert.Equal("永続化テスト", actual.Description);
            Assert.True(actual.IsActive);
        }
    }
}