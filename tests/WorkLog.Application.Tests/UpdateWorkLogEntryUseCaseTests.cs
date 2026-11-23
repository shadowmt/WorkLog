using Microsoft.Extensions.Time.Testing;
using Moq;
using WorkLog.Application.Abstractions;
using WorkLog.Application.Models;
using WorkLog.Application.UseCases;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.Tests;

public class UpdateWorkLogEntryUseCaseTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 23, 1, 0, 0, TimeSpan.Zero);

    private readonly Mock<IWorkLogEntryRepository> _workLogEntryRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();

    [Fact]
    public async Task ExecuteAsync_ShouldThrowKeyNotFoundException_WhenEntryDoesNotExist()
    {
        // Arrange
        var entryId = Guid.NewGuid();

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkLogEntry?)null);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entryId,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Unspecified,
            Optional<string>.Specified("更新後の説明"),
            Optional<int>.Unspecified);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);

        _projectRepository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private UpdateWorkLogEntryUseCase CreateUseCase()
        => new(
            _workLogEntryRepository.Object,
            _projectRepository.Object,
            new FakeTimeProvider(FixedNow));

    [Fact]
    public async Task ExecuteAsync_ShouldThrowKeyNotFoundException_WhenTargetProjectDoesNotExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow);

        var targetProjectId = Guid.NewGuid();

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                targetProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Specified(targetProjectId),
            Optional<string>.Unspecified,
            Optional<int>.Unspecified);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateEntry_WhenTargetProjectIsInactive()
    {
        // Arrange
        var currentProjectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            currentProjectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        var targetProject = new Project(
            "Inactive Project",
            FixedNow.AddMinutes(-2));

        targetProject.Deactivate(FixedNow.AddMinutes(-1));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                targetProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetProject);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Specified(targetProject.Id),
            Optional<string>.Unspecified,
            Optional<int>.Unspecified);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.Is<WorkLogEntry>(x =>
                    x.ProjectId == targetProject.Id &&
                    x.WorkDate == new DateOnly(2026, 11, 20) &&
                    x.Description == "既存の作業記録" &&
                    x.DurationMinutes == 60 &&
                    x.UpdatedAt == FixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _projectRepository.Verify(
            x => x.GetByIdAsync(
                targetProject.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateOnlyDescription_WhenOnlyDescriptionIsSpecified()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "変更前の説明",
            60,
            FixedNow.AddMinutes(-10));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Unspecified,
            Optional<string>.Specified("変更後の説明"),
            Optional<int>.Unspecified);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.Is<WorkLogEntry>(x =>
                    x.WorkDate == new DateOnly(2026, 11, 20) &&
                    x.ProjectId == projectId &&
                    x.Description == "変更後の説明" &&
                    x.DurationMinutes == 60 &&
                    x.UpdatedAt == FixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _projectRepository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateOnlyWorkDate_WhenOnlyWorkDateIsSpecified()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        var newWorkDate = new DateOnly(2026, 11, 21);

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Specified(newWorkDate),
            Optional<Guid>.Unspecified,
            Optional<string>.Unspecified,
            Optional<int>.Unspecified);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.Is<WorkLogEntry>(x =>
                    x.WorkDate == newWorkDate &&
                    x.ProjectId == projectId &&
                    x.Description == "既存の作業記録" &&
                    x.DurationMinutes == 60 &&
                    x.UpdatedAt == FixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateOnlyDurationMinutes_WhenOnlyDurationMinutesIsSpecified()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        const int newDurationMinutes = 120;

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Unspecified,
            Optional<string>.Unspecified,
            Optional<int>.Specified(newDurationMinutes));

        // Assert
        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.Is<WorkLogEntry>(x =>
                    x.WorkDate == new DateOnly(2026, 11, 20) &&
                    x.ProjectId == projectId &&
                    x.Description == "既存の作業記録" &&
                    x.DurationMinutes == newDurationMinutes &&
                    x.UpdatedAt == FixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateAllProperties_WhenAllPropertiesAreSpecified()
    {
        // Arrange
        var currentProjectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            currentProjectId,
            "変更前の説明",
            60,
            FixedNow.AddMinutes(-10));

        var targetProject = new Project(
            "Target Project",
            FixedNow.AddMinutes(-5));

        var newWorkDate = new DateOnly(2026, 11, 21);
        const string newDescription = "変更後の説明";
        const int newDurationMinutes = 120;

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                targetProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetProject);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Specified(newWorkDate),
            Optional<Guid>.Specified(targetProject.Id),
            Optional<string>.Specified(newDescription),
            Optional<int>.Specified(newDurationMinutes));

        // Assert
        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.Is<WorkLogEntry>(x =>
                    x.WorkDate == newWorkDate &&
                    x.ProjectId == targetProject.Id &&
                    x.Description == newDescription &&
                    x.DurationMinutes == newDurationMinutes &&
                    x.UpdatedAt == FixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateProjectId_WhenTargetProjectIsActive()
    {
        // Arrange
        var currentProjectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            currentProjectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        var targetProject = new Project(
            "Active Project",
            FixedNow.AddMinutes(-5));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                targetProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetProject);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Specified(targetProject.Id),
            Optional<string>.Unspecified,
            Optional<int>.Unspecified);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.Is<WorkLogEntry>(x =>
                    x.ProjectId == targetProject.Id &&
                    x.WorkDate == new DateOnly(2026, 11, 20) &&
                    x.Description == "既存の作業記録" &&
                    x.DurationMinutes == 60),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _projectRepository.Verify(
            x => x.GetByIdAsync(
                targetProject.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenNoPropertiesAreSpecified()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Unspecified,
            Optional<string>.Unspecified,
            Optional<int>.Unspecified);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _workLogEntryRepository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _projectRepository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSave_WhenDescriptionIsEmpty()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Unspecified,
            Optional<string>.Specified(" "),
            Optional<int>.Unspecified);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSave_WhenDurationMinutesIsZero()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Unspecified,
            Optional<string>.Unspecified,
            Optional<int>.Specified(0));

        // Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(act);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSave_WhenProjectIdIsEmpty()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Specified(Guid.Empty),
            Optional<string>.Unspecified,
            Optional<int>.Unspecified);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        _projectRepository.Verify(
            x => x.GetByIdAsync(
                Guid.Empty,
                It.IsAny<CancellationToken>()),
            Times.Never);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotSave_WhenWorkDateIsDefault()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Specified(default),
            Optional<Guid>.Unspecified,
            Optional<string>.Unspecified,
            Optional<int>.Unspecified);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotChangeEntry_WhenDescriptionIsEmpty()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var originalUpdatedAt = FixedNow.AddMinutes(-10);

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            originalUpdatedAt);

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Unspecified,
            Optional<string>.Specified(" "),
            Optional<int>.Unspecified);

        await Assert.ThrowsAsync<ArgumentException>(act);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 20), entry.WorkDate);
        Assert.Equal(projectId, entry.ProjectId);
        Assert.Equal("既存の作業記録", entry.Description);
        Assert.Equal(60, entry.DurationMinutes);
        Assert.Equal(originalUpdatedAt, entry.UpdatedAt);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassCancellationTokenToRepositories()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            FixedNow.AddMinutes(-10));

        var targetProject = new Project(
            "Target Project",
            FixedNow.AddMinutes(-5));

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entry.Id, cancellationToken))
            .ReturnsAsync(entry);

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                targetProject.Id, cancellationToken))
            .ReturnsAsync(targetProject);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            entry.Id,
            Optional<DateOnly>.Unspecified,
            Optional<Guid>.Specified(targetProject.Id),
            Optional<string>.Unspecified,
            Optional<int>.Unspecified,
            cancellationToken);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.GetByIdAsync(entry.Id, cancellationToken),
            Times.Once);

        _projectRepository.Verify(
            x => x.GetByIdAsync(targetProject.Id, cancellationToken),
            Times.Once);

        _workLogEntryRepository.Verify(
            x => x.UpdateAsync(entry, cancellationToken),
            Times.Once);
    }
}
