
using WorkLog.Domain.Entities;

namespace WorkLog.Domain.Tests;

public class WorkLogEntryTests
{
    private static readonly DateOnly ValidWorkDate = new(2026, 11, 18);
    private static readonly Guid ValidProjectId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 22, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ShouldCreateWorkLogEntry()
    {
        // Act
        var entry = new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            "  要件定義  ",
            90,
            FixedNow);

        // Assert
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(ValidWorkDate, entry.WorkDate);
        Assert.Equal(ValidProjectId, entry.ProjectId);
        Assert.Equal("要件定義", entry.Description);
        Assert.Equal(90, entry.DurationMinutes);
        Assert.Equal(FixedNow, entry.CreatedAt);
        Assert.Equal(FixedNow, entry.UpdatedAt);
    }

    [Fact]
    public void Constructor_ShouldNormalizeTimestampToUtc()
    {
        // Arrange
        var jstNow = new DateTimeOffset(
            2026, 11, 22, 10, 0, 0, TimeSpan.FromHours(9));

        // Act
        var entry = new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            "要件定義",
            60,
            jstNow);

        // Assert
        Assert.Equal(FixedNow, entry.CreatedAt);
        Assert.Equal(TimeSpan.Zero, entry.CreatedAt.Offset);
    }

    [Fact]
    public void Constructor_ShouldThrowWhenNowIsDefault()
    {
        // Act
        var act = () => new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            "要件定義",
            60,
            default);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowWhenWorkDateIsDefault()
    {
        // Act
        var act = () => new WorkLogEntry(
            default,
            ValidProjectId,
            "要件定義",
            60,
            FixedNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowWhenProjectIdIsEmpty()
    {
        // Act
        var act = () => new WorkLogEntry(
            ValidWorkDate,
            Guid.Empty,
            "要件定義",
            60,
            FixedNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowWhenDescriptionIsBlank(string description)
    {
        // Act
        var act = () => new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            description,
            60,
            FixedNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    public void Constructor_ShouldThrowWhenDurationIsZeroOrNegative(
        int durationMinutes)
    {
        // Act
        var act = () => new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            "要件定義",
            durationMinutes,
            FixedNow);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var entry = new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            "要件定義",
            60,
            FixedNow);

        var newDate = new DateOnly(2026, 11, 17);
        var newProjectId = Guid.NewGuid();
        var updatedAt = FixedNow.AddMinutes(5);

        // Act
        entry.Update(
            newDate,
            newProjectId,
            "  画面設計  ",
            120,
            updatedAt);

        // Assert
        Assert.Equal(newDate, entry.WorkDate);
        Assert.Equal(newProjectId, entry.ProjectId);
        Assert.Equal("画面設計", entry.Description);
        Assert.Equal(120, entry.DurationMinutes);
        Assert.Equal(FixedNow, entry.CreatedAt);
        Assert.Equal(updatedAt, entry.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldNotChangePropertiesWhenValidationFails()
    {
        // Arrange
        var entry = new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            "要件定義",
            60,
            FixedNow);

        // Act
        var act = () => entry.Update(
            new DateOnly(2026, 11, 17),
            Guid.NewGuid(),
            "画面設計",
            0,
            FixedNow.AddMinutes(5));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);

        Assert.Equal(ValidWorkDate, entry.WorkDate);
        Assert.Equal(ValidProjectId, entry.ProjectId);
        Assert.Equal("要件定義", entry.Description);
        Assert.Equal(60, entry.DurationMinutes);
        Assert.Equal(FixedNow, entry.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldNotChangePropertiesWhenNowIsDefault()
    {
        // Arrange
        var entry = new WorkLogEntry(
            ValidWorkDate,
            ValidProjectId,
            "要件定義",
            60,
            FixedNow);

        // Act
        var act = () => entry.Update(
            new DateOnly(2026, 11, 17),
            Guid.NewGuid(),
            "画面設計",
            120,
            default);

        // Assert
        Assert.Throws<ArgumentException>(act);

        Assert.Equal(ValidWorkDate, entry.WorkDate);
        Assert.Equal(ValidProjectId, entry.ProjectId);
        Assert.Equal("要件定義", entry.Description);
        Assert.Equal(60, entry.DurationMinutes);
        Assert.Equal(FixedNow, entry.UpdatedAt);
    }
}
