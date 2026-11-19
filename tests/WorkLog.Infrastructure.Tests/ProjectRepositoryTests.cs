using Microsoft.EntityFrameworkCore;
using WorkLog.Domain.Entities;
using WorkLog.Infrastructure.Repositories;

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
    public async Task Add_ShouldPersistProject()
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

    [Fact]
    public async Task Update_ShouldPersistUpdatedProject()
    {
        // Arrange
        var project = new Project(
            $"更新テスト_{Guid.NewGuid():N}",
            "更新前の説明");
        var updatedName = $"更新後のプロジェクト名_{Guid.NewGuid():N}";

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = fixture.CreateDbContext())
        {
            var target = await dbContext.Projects
                .SingleAsync(x => x.Id == project.Id);

            target.Update(updatedName, "更新後の説明");

            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            var actual = await dbContext.Projects
                .SingleAsync(x => x.Id == project.Id);

            Assert.Equal(updatedName, actual.Name);
            Assert.Equal(updatedName.ToUpperInvariant(), actual.NormalizedName);
            Assert.Equal("更新後の説明", actual.Description);
        }
    }

    [Fact]
    public async Task Deactivate_ShouldPersistInactiveProject()
    {
        // Arrange
        var project = new Project(
            $"無効化テスト_{Guid.NewGuid():N}",
            "無効化の永続化テスト");

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = fixture.CreateDbContext())
        {
            var target = await dbContext.Projects
                .SingleAsync(x => x.Id == project.Id);

            target.Deactivate();

            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            var actual = await dbContext.Projects
                .SingleAsync(x => x.Id == project.Id);

            Assert.False(actual.IsActive);
        }
    }

    [Fact]
    public async Task ActivateAsync_ShouldPersistActiveProject()
    {
        // Arrange
        var project = new Project(
            $"有効化テスト_{Guid.NewGuid():N}",
            "有効化の永続化テスト");

        project.Deactivate();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = fixture.CreateDbContext())
        {
            var target = await dbContext.Projects
                .SingleAsync(x => x.Id == project.Id);

            target.Activate();

            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            var actual = await dbContext.Projects
                .SingleAsync(x => x.Id == project.Id);

            Assert.True(actual.IsActive);
        }
    }

    [Fact]
    public async Task AddAsync_ShouldRejectDuplicateNormalizedName()
    {
        // Arrange
        var name = $"重複テスト_{Guid.NewGuid():N}";

        var first = new Project(name, "最初のプロジェクト");
        var second = new Project(name.ToLowerInvariant(), "重複するプロジェクト");

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(first);
            await dbContext.SaveChangesAsync();
        }

        // Act & Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(second);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => dbContext.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProject_WhenProjectExists()
    {
        // Arrange
        var project = new Project(
            $"リポジトリテスト_{Guid.NewGuid():N}",
            "GetByIdAsync検索テスト");

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            // Act
            var repository = new ProjectRepository(dbContext);
            var result = await repository.GetByIdAsync(project.Id);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(project.Id, result.Id);
            Assert.Equal(project.Name, result.Name);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}
