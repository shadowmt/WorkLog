using Moq;
using WorkLog.Application.Abstractions;
using WorkLog.Application.UseCases;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.Tests.UseCases;

public class DeleteWorkLogEntryUseCaseTests
{
    private readonly Mock<IWorkLogEntryRepository> _workLogEntryRepository = new();

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteEntry_WhenEntryExists()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            new DateTimeOffset(2026, 11, 23, 1, 0, 0, TimeSpan.Zero));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(entryId);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.DeleteAsync(
                entry,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private DeleteWorkLogEntryUseCase CreateUseCase()
        => new(_workLogEntryRepository.Object);

    [Fact]
    public async Task ExecuteAsync_ShouldThrowKeyNotFoundException_WhenEntryDoesNotExist()
    {
        // Arrange
        var entryId = Guid.NewGuid();

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkLogEntry?)null);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(entryId);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);

        _workLogEntryRepository.Verify(
            x => x.DeleteAsync(
                It.IsAny<WorkLogEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var entry = new WorkLogEntry(
            new DateOnly(2026, 11, 20),
            projectId,
            "既存の作業記録",
            60,
            new DateTimeOffset(2026, 11, 23, 1, 0, 0, TimeSpan.Zero));

        _workLogEntryRepository
            .Setup(x => x.GetByIdAsync(
                entryId,
                cancellationToken))
            .ReturnsAsync(entry);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(entryId, cancellationToken);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.GetByIdAsync(entryId, cancellationToken),
            Times.Once);

        _workLogEntryRepository.Verify(
            x => x.DeleteAsync(entry, cancellationToken),
            Times.Once);
    }
}
