using Moq;
using WorkLog.Application.Abstractions;
using WorkLog.Application.Models;
using WorkLog.Application.UseCases;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.Tests.UseCases;

public class GetWorkLogEntriesUseCaseTests
{
    private readonly Mock<IWorkLogEntryRepository> _workLogEntryRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPagedResult_WhenCriteriaIsValid()
    {
        // Arrange
        var criteria = new WorkLogEntrySearchCriteria(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            null,
            1,
            50);

        var expected = new PagedResult<WorkLogEntryListItem>(
            new List<WorkLogEntryListItem>
            {
                new(
                    Guid.NewGuid(),
                    new DateOnly(2026, 11, 19),
                    Guid.NewGuid(),
                    "Test Project",
                    true,
                    "Application層のテスト",
                    60)
            },
            1,
            50,
            1,
            1);

        _workLogEntryRepository
            .Setup(x => x.GetPagedAsync(
                criteria,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var useCase = CreateUseCase();

        // Act
        var result = await useCase.ExecuteAsync(criteria);

        // Assert
        Assert.Equal(expected, result);
        Assert.Single(result.Items);
        Assert.Equal("Test Project", result.Items[0].ProjectName);

        _workLogEntryRepository.Verify(
            x => x.GetPagedAsync(
                criteria,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenFromIsLaterThanTo()
    {
        // Arrange
        var criteria = new WorkLogEntrySearchCriteria(
            new DateOnly(2026, 11, 30),
            new DateOnly(2026, 11, 1),
            null,
            1,
            50);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(criteria);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        _workLogEntryRepository.Verify(
            x => x.GetPagedAsync(
                It.IsAny<WorkLogEntrySearchCriteria>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenPageIsLessThanOne()
    {
        // Arrange
        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            null,
            0,
            50);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(criteria);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        _workLogEntryRepository.Verify(
            x => x.GetPagedAsync(
                It.IsAny<WorkLogEntrySearchCriteria>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenPageSizeIsOutOfRange(
        int pageSize)
    {
        // Arrange
        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            null,
            1,
            pageSize);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(criteria);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        _workLogEntryRepository.Verify(
            x => x.GetPagedAsync(
                It.IsAny<WorkLogEntrySearchCriteria>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            null,
            1,
            50);

        var expected = new PagedResult<WorkLogEntryListItem>(
            Array.Empty<WorkLogEntryListItem>(),
            1,
            50,
            0,
            0);

        _workLogEntryRepository
            .Setup(x => x.GetPagedAsync(criteria, cancellationToken))
            .ReturnsAsync(expected);

        var useCase = CreateUseCase();

        // Act
        await useCase.ExecuteAsync(criteria, cancellationToken);

        // Assert
        _workLogEntryRepository.Verify(
            x => x.GetPagedAsync(criteria, cancellationToken),
            Times.Once);
    }

    private GetWorkLogEntriesUseCase CreateUseCase()
        => new(
            _projectRepository.Object,
            _workLogEntryRepository.Object);

    [Fact]
    public async Task ExecuteAsync_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var criteria = new WorkLogEntrySearchCriteria(
            null,
            null,
            projectId,
            1,
            50);

        _projectRepository
            .Setup(x => x.GetByIdAsync(
                projectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var useCase = CreateUseCase();

        // Act
        Func<Task> act = () => useCase.ExecuteAsync(criteria);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);

        _workLogEntryRepository.Verify(
            x => x.GetPagedAsync(
                It.IsAny<WorkLogEntrySearchCriteria>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
