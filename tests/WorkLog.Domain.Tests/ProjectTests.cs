
using System;
using WorkLog.Domain.Entities;

namespace WorkLog.Domain.Tests;

public class ProjectTests
{
    [Fact]
    public void Constructor_ShouldNormalizeNameAndDescription()
    {
        // Arrange
        var name = "  WorkLog  ";
        var description = "  作業実績管理アプリ  ";

        // Act
        var project = new Project(name, description);

        // Assert
        Assert.Equal("WorkLog", project.Name);
        Assert.Equal("WORKLOG", project.NormalizedName);
        Assert.Equal("作業実績管理アプリ", project.Description);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var project = new Project("WorkLog");

        // Assert
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.True(project.IsActive);
        Assert.Null(project.Description);
        Assert.Equal(project.CreatedAt, project.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, project.CreatedAt.Offset);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowWhenNameIsBlank(string name)
    {
        // Act
        var act = () => new Project(name);

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
        var project = new Project("WorkLog", description);

        // Assert
        Assert.Null(project.Description);
    }

    [Fact]
    public void Update_ShouldNormalizeNameAndDescription()
    {
        // Arrange
        var project = new Project("OldName");

        // Act
        project.Update("  NewName  ", "  新しい説明  ");

        // Assert
        Assert.Equal("NewName", project.Name);
        Assert.Equal("NEWNAME", project.NormalizedName);
        Assert.Equal("新しい説明", project.Description);
    }

    [Fact]
    public void Update_ShouldSetDescriptionToNullWhenBlank()
    {
        // Arrange
        var project = new Project("WorkLog", "説明");

        // Act
        project.Update("WorkLog", " ");

        // Assert
        Assert.Null(project.Description);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var project = new Project("WorkLog");

        // Act
        project.Deactivate();

        // Assert
        Assert.False(project.IsActive);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var project = new Project("WorkLog");
        project.Deactivate();

        // Act
        project.Activate();

        // Assert
        Assert.True(project.IsActive);
    }
}
