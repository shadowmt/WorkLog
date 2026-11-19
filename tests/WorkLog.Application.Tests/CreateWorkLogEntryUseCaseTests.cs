using WorkLog.Application.UseCases;
using Moq;
using WorkLog.Application.Abstractions;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.Tests;

public class CreateWorkLogEntryUseCaseTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IWorkLogEntryRepository> _workLogEntryRepository = new();

    [Fact]
    public async Task ExecuteAsync_ShouldRegisterEntry_WhenProjectIsActive()
    {
        // Arrange
        var project = new Project("Test Project", null);
        _projectRepository
            .Setup(x => x.GetByIdAsync(
                project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var useCase = CreateUseCase();

        var workDate = new DateOnly(2026, 11, 19);
        const string description = "Application層のテスト";
        const int durationMinutes = 60;

        // Act
        await useCase.ExecuteAsync(
            workDate,
            project.Id,
            description,
            durationMinutes);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.AddAsync(
                It.Is<WorkLogEntry>(entry =>
                    entry.ProjectId == project.Id &&
                    entry.WorkDate == workDate &&
                    entry.Description == description &&
                    entry.DurationMinutes == durationMinutes),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowInvalidOperationException_WhenProjectIsInactive()
    {
        // Arrange
        var project = new Project("Inactive Project", null);
        project.Deactivate();

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            new DateOnly(2026, 11, 19),
            project.Id,
            "無効Projectへの登録",
            60);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);

        _workLogEntryRepository.Verify(
            x => x.AddAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(
            new DateOnly(2026, 11, 19),
            projectId,
            "存在しないProjectへの登録",
            60);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);

        _workLogEntryRepository.Verify(
            x => x.AddAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private CreateWorkLogEntryUseCase CreateUseCase()
        => new(
            _projectRepository.Object,
            _workLogEntryRepository.Object);

    [Fact]
    public async Task ExecuteAsync_ShouldPassCancellationTokenToRepositories()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var project = new Project("Test Project", null);

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                project.Id, cancellationToken))
            .ReturnsAsync(project);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(
            new DateOnly(2026, 11, 19),
            project.Id,
            "CancellationTokenのテスト",
            60,
            cancellationToken);

        // Assert
        _projectRepository.Verify(
            x => x.GetByIdAsync(project.Id, cancellationToken),
            Times.Once);

        _workLogEntryRepository.Verify(
            x => x.AddAsync(
                It.IsAny<WorkLogEntry>(),
                cancellationToken),
            Times.Once);
    }
}
