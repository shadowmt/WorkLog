
using Microsoft.EntityFrameworkCore;
using WorkLog.Domain.Entities;

namespace WorkLog.Infrastructure.Tests;

public class WorkLogEntryRepositoryTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task AddAsync_ShouldPersistWorkLogEntry()
    {
        // Arrange
        var project = new Project(
            $"作業実績テスト_{Guid.NewGuid():N}",
            "作業実績の永続化テスト");

        var workDate = new DateOnly(2026, 11, 19);
        var description = "WorkLogEntryの永続化テスト";
        const int durationMinutes = 90;

        Guid workLogEntryId;

        // Projectを登録
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
        }

        var entry = new WorkLogEntry(
            workDate,
            project.Id,
            description,
            durationMinutes);

        workLogEntryId = entry.Id;

        // Act
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.WorkLogEntries.Add(entry);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            var actual = await dbContext.WorkLogEntries
                .Include(x => x.Project)
                .SingleAsync(x => x.Id == workLogEntryId);

            Assert.Equal(workDate, actual.WorkDate);
            Assert.Equal(project.Id, actual.ProjectId);
            Assert.Equal(description, actual.Description);
            Assert.Equal(durationMinutes, actual.DurationMinutes);
            Assert.Equal(project.Name, actual.Project.Name);
        }
    }


    [Fact]
    public async Task UpdateAsync_ShouldPersistUpdatedWorkLogEntry()
    {
        // Arrange
        var project1 = new Project(
            $"更新前プロジェクト_{Guid.NewGuid():N}",
            "更新前");

        var project2 = new Project(
            $"更新後プロジェクト_{Guid.NewGuid():N}",
            "更新後");

        var originalDate = new DateOnly(2026, 11, 18);
        var updatedDate = new DateOnly(2026, 11, 19);

        var entry = new WorkLogEntry(
            originalDate,
            project1.Id,
            "更新前の作業内容",
            60);

        // ProjectとWorkLogEntryを登録
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.AddRange(project1, project2);
            dbContext.WorkLogEntries.Add(entry);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = fixture.CreateDbContext())
        {
            var target = await dbContext.WorkLogEntries
                .SingleAsync(x => x.Id == entry.Id);

            target.Update(
                updatedDate,
                project2.Id,
                "更新後の作業内容",
                120);

            await dbContext.SaveChangesAsync();
        }

        // Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            var actual = await dbContext.WorkLogEntries
                .Include(x => x.Project)
                .SingleAsync(x => x.Id == entry.Id);

            Assert.Equal(updatedDate, actual.WorkDate);
            Assert.Equal(project2.Id, actual.ProjectId);
            Assert.Equal("更新後の作業内容", actual.Description);
            Assert.Equal(120, actual.DurationMinutes);
            Assert.Equal(project2.Name, actual.Project.Name);
        }
    }


    [Fact]
    public async Task DeactivateProject_ShouldPreserveExistingWorkLogEntries()
    {
        // Arrange
        var project = new Project(
            $"無効化関連テスト_{Guid.NewGuid():N}",
            "無効化後も作業実績を保持するテスト");

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 19),
            project.Id,
            "既存の作業実績",
            90);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            dbContext.WorkLogEntries.Add(entry);
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
            var actual = await dbContext.WorkLogEntries
                .Include(x => x.Project)
                .SingleAsync(x => x.Id == entry.Id);

            Assert.Equal(entry.Id, actual.Id);
            Assert.Equal(project.Id, actual.ProjectId);
            Assert.Equal("既存の作業実績", actual.Description);
            Assert.False(actual.Project.IsActive);
        }
    }
}
