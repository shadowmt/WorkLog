using System.Text.Json;
using WorkLog.Api.Contracts.WorkLogEntries;

namespace WorkLog.Api.Tests;

public class UpdateWorkLogEntryRequestParserTests
{
    [Fact]
    public void Parse_ShouldReturnAllUnspecified_WhenPropertiesAreOmitted()
    {
        // Arrange
        using var document = JsonDocument.Parse("{}");

        // Act
        var request = UpdateWorkLogEntryRequestParser.Parse(
            document.RootElement);

        // Assert
        Assert.False(request.WorkDate.IsSpecified);
        Assert.False(request.ProjectId.IsSpecified);
        Assert.False(request.Description.IsSpecified);
        Assert.False(request.DurationMinutes.IsSpecified);
    }

    [Fact]
    public void Parse_ShouldSetOnlySpecifiedProperty_WhenDescriptionIsProvided()
    {
        // Arrange
        using var document = JsonDocument.Parse(
            """{"description":"設計書を作成"}""");

        // Act
        var request = UpdateWorkLogEntryRequestParser.Parse(
            document.RootElement);

        // Assert
        Assert.False(request.WorkDate.IsSpecified);
        Assert.False(request.ProjectId.IsSpecified);

        Assert.True(request.Description.IsSpecified);
        Assert.Equal("設計書を作成", request.Description.Value);

        Assert.False(request.DurationMinutes.IsSpecified);
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenDescriptionIsNull()
    {
        // Arrange
        using var document = JsonDocument.Parse(
            """{"description":null}""");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UpdateWorkLogEntryRequestParser.Parse(
                document.RootElement));
    }

    [Theory]
    [InlineData("""{"description":123}""")]
    [InlineData("""{"description":true}""")]
    [InlineData("""{"description":{}}""")]
    [InlineData("""{"description":[]}""")]
    public void Parse_ShouldThrowArgumentException_WhenDescriptionIsNotString(
        string json)
    {
        // Arrange
        using var document = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UpdateWorkLogEntryRequestParser.Parse(
                document.RootElement));
    }

    [Fact]
    public void Parse_ShouldSetWorkDate_WhenValidDateIsProvided()
    {
        // Arrange
        using var document = JsonDocument.Parse(
            """{"workDate":"2026-11-23"}""");

        var expected = new DateOnly(2026, 11, 23);

        // Act
        var request = UpdateWorkLogEntryRequestParser.Parse(
            document.RootElement);

        // Assert
        Assert.True(request.WorkDate.IsSpecified);
        Assert.Equal(expected, request.WorkDate.Value);
    }

    [Theory]
    [InlineData("""{"workDate":"2026-13-01"}""")]
    [InlineData("""{"workDate":"2026-02-30"}""")]
    [InlineData("""{"workDate":"not-a-date"}""")]
    public void Parse_ShouldThrowArgumentException_WhenWorkDateIsInvalid(
        string json)
    {
        // Arrange
        using var document = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UpdateWorkLogEntryRequestParser.Parse(
                document.RootElement));
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenWorkDateIsNull()
    {
        // Arrange
        using var document = JsonDocument.Parse(
            """{"workDate":null}""");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UpdateWorkLogEntryRequestParser.Parse(
                document.RootElement));
    }

    [Fact]
    public void Parse_ShouldSetProjectId_WhenValidGuidIsProvided()
    {
        // Arrange
        var expected = Guid.NewGuid();

        using var document = JsonDocument.Parse(
            $$"""{"projectId":"{{expected}}"}""");

        // Act
        var request = UpdateWorkLogEntryRequestParser.Parse(
            document.RootElement);

        // Assert
        Assert.True(request.ProjectId.IsSpecified);
        Assert.Equal(expected, request.ProjectId.Value);
    }

    [Theory]
    [InlineData("""{"projectId":"not-a-guid"}""")]
    [InlineData("""{"projectId":123}""")]
    [InlineData("""{"projectId":true}""")]
    [InlineData("""{"projectId":null}""")]
    public void Parse_ShouldThrowArgumentException_WhenProjectIdIsInvalid(
        string json)
    {
        // Arrange
        using var document = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UpdateWorkLogEntryRequestParser.Parse(
                document.RootElement));
    }

    [Fact]
    public void Parse_ShouldSetDurationMinutes_WhenValidValueIsProvided()
    {
        // Arrange
        using var document = JsonDocument.Parse(
            """{"durationMinutes":120}""");

        // Act
        var request = UpdateWorkLogEntryRequestParser.Parse(
            document.RootElement);

        // Assert
        Assert.True(request.DurationMinutes.IsSpecified);
        Assert.Equal(120, request.DurationMinutes.Value);
    }

    [Theory]
    [InlineData("""{"durationMinutes":0}""")]
    [InlineData("""{"durationMinutes":1441}""")]
    [InlineData("""{"durationMinutes":-1}""")]
    [InlineData("""{"durationMinutes":1.5}""")]
    [InlineData("""{"durationMinutes":"120"}""")]
    [InlineData("""{"durationMinutes":null}""")]
    public void Parse_ShouldThrowArgumentException_WhenDurationMinutesIsInvalid(
        string json)
    {
        // Arrange
        using var document = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UpdateWorkLogEntryRequestParser.Parse(
                document.RootElement));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("123")]
    [InlineData("\"text\"")]
    [InlineData("true")]
    public void Parse_ShouldThrowArgumentException_WhenRootIsNotObject(
        string json)
    {
        // Arrange
        using var document = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UpdateWorkLogEntryRequestParser.Parse(
                document.RootElement));
    }

    [Fact]
    public void Parse_ShouldSetAllProperties_WhenAllValidPropertiesAreProvided()
    {
        // Arrange
        var expectedDate = new DateOnly(2026, 11, 23);
        var expectedProjectId = Guid.NewGuid();

        using var document = JsonDocument.Parse(
            $$"""
        {
            "workDate": "{{expectedDate:yyyy-MM-dd}}",
            "projectId": "{{expectedProjectId}}",
            "description": "設計書を作成",
            "durationMinutes": 120
        }
        """);

        // Act
        var request = UpdateWorkLogEntryRequestParser.Parse(
            document.RootElement);

        // Assert
        Assert.True(request.WorkDate.IsSpecified);
        Assert.Equal(expectedDate, request.WorkDate.Value);

        Assert.True(request.ProjectId.IsSpecified);
        Assert.Equal(expectedProjectId, request.ProjectId.Value);

        Assert.True(request.Description.IsSpecified);
        Assert.Equal("設計書を作成", request.Description.Value);

        Assert.True(request.DurationMinutes.IsSpecified);
        Assert.Equal(120, request.DurationMinutes.Value);
    }
}
