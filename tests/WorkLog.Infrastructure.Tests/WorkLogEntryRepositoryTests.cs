using Microsoft.EntityFrameworkCore;
using WorkLog.Domain.Entities;
using WorkLog.Application.Models;
using WorkLog.Infrastructure.Repositories;

namespace WorkLog.Infrastructure.Tests;

public class WorkLogEntryRepositoryTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 22, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_ShouldPersistWorkLogEntry()
    {
        // Arrange
        var project = new Project(
            $"作業実績テスト_{Guid.NewGuid():N}",
            FixedNow,
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
            durationMinutes,
            FixedNow);

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
            FixedNow,
            "更新前");

        var project2 = new Project(
            $"更新後プロジェクト_{Guid.NewGuid():N}",
            FixedNow,
            "更新後");

        var originalDate = new DateOnly(2026, 11, 18);
        var updatedDate = new DateOnly(2026, 11, 19);

        var entry = new WorkLogEntry(
            originalDate,
            project1.Id,
            "更新前の作業内容",
            60,
            FixedNow);

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
                120,
                FixedNow);

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
            FixedNow,
            "無効化後も作業実績を保持するテスト");

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 19),
            project.Id,
            "既存の作業実績",
            90,
            FixedNow);

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

            target.Deactivate(FixedNow);

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

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResultsOrderedByWorkDateDescending()
    {
        // Arrange
        var project = new Project(
            $"ページングテスト_{Guid.NewGuid():N}",
            FixedNow,
            "ページングのテスト");

        var entry1 = new WorkLogEntry(
            new DateOnly(2026, 11, 18),
            project.Id,
            "1件目の作業",
            60,
            FixedNow);

        var entry2 = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            project.Id,
            "2件目の作業",
            90,
            FixedNow);

        var entry3 = new WorkLogEntry(
            new DateOnly(2026, 11, 19),
            project.Id,
            "3件目の作業",
            120,
            FixedNow);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);

            dbContext.WorkLogEntries.AddRange(
                entry1,
                entry2,
                entry3);

            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            project.Id,
            1,
            2);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);

            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        Assert.Equal(2, actual.Items.Count);
        Assert.Equal(1, actual.Page);
        Assert.Equal(2, actual.PageSize);
        Assert.Equal(3, actual.TotalCount);
        Assert.Equal(2, actual.TotalPages);

        Assert.Equal(entry2.Id, actual.Items[0].Id);
        Assert.Equal(entry3.Id, actual.Items[1].Id);

        Assert.All(actual.Items, item =>
        {
            Assert.Equal(project.Name, item.ProjectName);
            Assert.True(item.ProjectIsActive);
        });
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var project = new Project(
            $"日付範囲テスト_{Guid.NewGuid():N}",
            FixedNow,
            "日付範囲の検索テスト");

        var entryBefore = new WorkLogEntry(
            new DateOnly(2026, 11, 9),
            project.Id,
            "開始日より前",
            60,
            FixedNow);

        var entryFrom = new WorkLogEntry(
            new DateOnly(2026, 11, 10),
            project.Id,
            "開始日当日",
            60,
            FixedNow);

        var entryMiddle = new WorkLogEntry(
            new DateOnly(2026, 11, 15),
            project.Id,
            "期間内",
            60,
            FixedNow);

        var entryTo = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            project.Id,
            "終了日当日",
            60,
            FixedNow);

        var entryAfter = new WorkLogEntry(
            new DateOnly(2026, 11, 21),
            project.Id,
            "終了日より後",
            60,
            FixedNow);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);

            dbContext.WorkLogEntries.AddRange(
                entryBefore,
                entryFrom,
                entryMiddle,
                entryTo,
                entryAfter);

            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            new DateOnly(2026, 11, 10),
            new DateOnly(2026, 11, 20),
            project.Id,
            1,
            50);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);

            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        Assert.Equal(3, actual.TotalCount);
        Assert.Equal(1, actual.TotalPages);
        Assert.Equal(3, actual.Items.Count);

        Assert.Equal(
            new[] { entryTo.Id, entryMiddle.Id, entryFrom.Id },
            actual.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByFromDateOnly()
    {
        // Arrange
        var project = new Project(
            $"開始日検索テスト_{Guid.NewGuid():N}",
            FixedNow,
            "開始日のみの検索テスト");

        var entryBefore = new WorkLogEntry(
            new DateOnly(2026, 11, 9),
            project.Id,
            "開始日より前",
            60,
            FixedNow);

        var entryFrom = new WorkLogEntry(
            new DateOnly(2026, 11, 10),
            project.Id,
            "開始日当日",
            60,
            FixedNow);

        var entryAfter = new WorkLogEntry(
            new DateOnly(2026, 11, 15),
            project.Id,
            "開始日より後",
            60,
            FixedNow);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            dbContext.WorkLogEntries.AddRange(
                entryBefore,
                entryFrom,
                entryAfter);

            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            new DateOnly(2026, 11, 10),
            null,
            project.Id,
            1,
            50);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);
            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        Assert.Equal(2, actual.TotalCount);
        Assert.Equal(
            new[] { entryAfter.Id, entryFrom.Id },
            actual.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByToDateOnly()
    {
        // Arrange
        var project = new Project(
            $"終了日検索テスト_{Guid.NewGuid():N}",
            FixedNow,
            "終了日のみの検索テスト");

        var entryBefore = new WorkLogEntry(
            new DateOnly(2026, 11, 9),
            project.Id,
            "終了日より前",
            60,
            FixedNow);

        var entryTo = new WorkLogEntry(
            new DateOnly(2026, 11, 10),
            project.Id,
            "終了日当日",
            60,
            FixedNow);

        var entryAfter = new WorkLogEntry(
            new DateOnly(2026, 11, 11),
            project.Id,
            "終了日より後",
            60,
            FixedNow);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            dbContext.WorkLogEntries.AddRange(
                entryBefore,
                entryTo,
                entryAfter);

            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            new DateOnly(2026, 11, 10),
            project.Id,
            1,
            50);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);
            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        Assert.Equal(2, actual.TotalCount);
        Assert.Equal(
            new[] { entryTo.Id, entryBefore.Id },
            actual.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByProjectId()
    {
        // Arrange
        var project1 = new Project(
            $"Project検索テスト1_{Guid.NewGuid():N}",
            FixedNow,
            "対象Project");

        var project2 = new Project(
            $"Project検索テスト2_{Guid.NewGuid():N}",
            FixedNow,
            "対象外Project");

        var entry1 = new WorkLogEntry(
            new DateOnly(2026, 11, 19),
            project1.Id,
            "対象Projectの作業",
            60,
            FixedNow);

        var entry2 = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            project2.Id,
            "対象外Projectの作業",
            90,
            FixedNow);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.AddRange(project1, project2);
            dbContext.WorkLogEntries.AddRange(entry1, entry2);
            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            project1.Id,
            1,
            50);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);
            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        var item = Assert.Single(actual.Items);

        Assert.Equal(entry1.Id, item.Id);
        Assert.Equal(project1.Id, item.ProjectId);
        Assert.Equal(project1.Name, item.ProjectName);
        Assert.Equal(1, actual.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnEntriesForInactiveProject()
    {
        // Arrange
        var project = new Project(
            $"無効Project検索テスト_{Guid.NewGuid():N}",
            FixedNow,
            "無効Projectの検索テスト");

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 19),
            project.Id,
            "無効化前の作業実績",
            90,
            FixedNow);

        project.Deactivate(FixedNow);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            dbContext.WorkLogEntries.Add(entry);
            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            project.Id,
            1,
            50);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);
            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        var item = Assert.Single(actual.Items);

        Assert.Equal(entry.Id, item.Id);
        Assert.Equal(project.Id, item.ProjectId);
        Assert.Equal(project.Name, item.ProjectName);
        Assert.False(item.ProjectIsActive);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnEmptyResult_WhenNoEntriesMatch()
    {
        // Arrange
        var project = new Project(
            $"0件検索テスト_{Guid.NewGuid():N}",
            FixedNow,
            "0件検索のテスト");

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            project.Id,
            1,
            50);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);
            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        Assert.Empty(actual.Items);
        Assert.Equal(1, actual.Page);
        Assert.Equal(50, actual.PageSize);
        Assert.Equal(0, actual.TotalCount);
        Assert.Equal(0, actual.TotalPages);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnEmptyItems_WhenPageExceedsTotalPages()
    {
        // Arrange
        var project = new Project(
            $"範囲外ページテスト_{Guid.NewGuid():N}",
            FixedNow,
            "範囲外ページのテスト");

        var entry1 = new WorkLogEntry(
            new DateOnly(2026, 11, 18),
            project.Id,
            "1件目",
            60,
            FixedNow);

        var entry2 = new WorkLogEntry(
            new DateOnly(2026, 11, 19),
            project.Id,
            "2件目",
            60,
            FixedNow);

        var entry3 = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            project.Id,
            "3件目",
            60,
            FixedNow);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            dbContext.WorkLogEntries.AddRange(
                entry1,
                entry2,
                entry3);

            await dbContext.SaveChangesAsync();
        }

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            project.Id,
            3,
            2);

        // Act
        PagedResult<WorkLogEntryListItem> actual;

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);
            actual = await repository.GetPagedAsync(criteria);
        }

        // Assert
        Assert.Empty(actual.Items);
        Assert.Equal(3, actual.Page);
        Assert.Equal(2, actual.PageSize);
        Assert.Equal(3, actual.TotalCount);
        Assert.Equal(2, actual.TotalPages);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveWorkLogEntry()
    {
        // Arrange
        var project = new Project(
            $"削除テスト_{Guid.NewGuid():N}",
            FixedNow,
            "作業実績の削除テスト");

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 19),
            project.Id,
            "削除対象の作業記録",
            60,
            FixedNow);

        var entryId = entry.Id;

        // テストデータを登録
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Projects.Add(project);
            dbContext.WorkLogEntries.Add(entry);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new WorkLogEntryRepository(dbContext);

            var target = await repository.GetByIdAsync(entryId);

            Assert.NotNull(target);

            await repository.DeleteAsync(target);
        }

        // Assert
        await using (var dbContext = fixture.CreateDbContext())
        {
            var actual = await dbContext.WorkLogEntries
                .SingleOrDefaultAsync(x => x.Id == entryId);

            Assert.Null(actual);
        }
    }
}
