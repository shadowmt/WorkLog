
using WorkLog.Domain.Entities;

namespace WorkLog.Domain.Tests;

public class ProjectTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 22, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ShouldNormalizeNameAndDescription()
    {
        // Arrange
        var name = "  WorkLog  ";
        var description = "  作業実績管理アプリ  ";

        // Act
        var project = new Project(name, FixedNow, description);

        // Assert
        Assert.Equal("WorkLog", project.Name);
        Assert.Equal("WORKLOG", project.NormalizedName);
        Assert.Equal("作業実績管理アプリ", project.Description);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var project = new Project("WorkLog", FixedNow);

        // Assert
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.True(project.IsActive);
        Assert.Null(project.Description);
        Assert.Equal(FixedNow, project.CreatedAt);
        Assert.Equal(FixedNow, project.UpdatedAt);
    }

    [Fact]
    public void Constructor_ShouldNormalizeTimestampToUtc()
    {
        // Arrange
        var jstNow = new DateTimeOffset(
            2026, 11, 22, 10, 0, 0, TimeSpan.FromHours(9));

        // Act
        var project = new Project("WorkLog", jstNow);

        // Assert
        Assert.Equal(FixedNow, project.CreatedAt);
        Assert.Equal(TimeSpan.Zero, project.CreatedAt.Offset);
    }

    [Fact]
    public void Constructor_ShouldThrowWhenNowIsDefault()
    {
        // Act
        var act = () => new Project("WorkLog", default);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowWhenNameIsBlank(string name)
    {
        // Act
        var act = () => new Project(name, FixedNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_ShouldSetDescriptionToNullWhenBlank(string description)
    {
        // Act
        var project = new Project("WorkLog", FixedNow, description);

        // Assert
        Assert.Null(project.Description);
    }

    [Fact]
    public void Update_ShouldNormalizeNameAndDescription()
    {
        // Arrange
        var project = new Project("OldName", FixedNow);
        var updatedAt = FixedNow.AddMinutes(5);

        // Act
        project.Update("  NewName  ", "  新しい説明  ", updatedAt);

        // Assert
        Assert.Equal("NewName", project.Name);
        Assert.Equal("NEWNAME", project.NormalizedName);
        Assert.Equal("新しい説明", project.Description);
        Assert.Equal(updatedAt, project.UpdatedAt);
        Assert.Equal(FixedNow, project.CreatedAt);
    }

    [Fact]
    public void Update_ShouldSetDescriptionToNullWhenBlank()
    {
        // Arrange
        var project = new Project("WorkLog", FixedNow, "説明");

        // Act
        project.Update("WorkLog", " ", FixedNow.AddMinutes(5));

        // Assert
        Assert.Null(project.Description);
    }

    [Fact]
    public void Update_ShouldNotChangePropertiesWhenNowIsDefault()
    {
        // Arrange
        var project = new Project("WorkLog", FixedNow, "元の説明");

        // Act
        var act = () => project.Update("NewName", "新しい説明", default);

        // Assert
        Assert.Throws<ArgumentException>(act);

        Assert.Equal("WorkLog", project.Name);
        Assert.Equal("WORKLOG", project.NormalizedName);
        Assert.Equal("元の説明", project.Description);
        Assert.Equal(FixedNow, project.UpdatedAt);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalseAndUpdateTimestamp()
    {
        // Arrange
        var project = new Project("WorkLog", FixedNow);
        var updatedAt = FixedNow.AddMinutes(5);

        // Act
        project.Deactivate(updatedAt);

        // Assert
        Assert.False(project.IsActive);
        Assert.Equal(FixedNow, project.CreatedAt);
        Assert.Equal(updatedAt, project.UpdatedAt);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrueAndUpdateTimestamp()
    {
        // Arrange
        var project = new Project("WorkLog", FixedNow);
        project.Deactivate(FixedNow.AddMinutes(5));
        var updatedAt = FixedNow.AddMinutes(10);

        // Act
        project.Activate(updatedAt);

        // Assert
        Assert.True(project.IsActive);
        Assert.Equal(FixedNow, project.CreatedAt);
        Assert.Equal(updatedAt, project.UpdatedAt);
    }
}
