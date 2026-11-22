using Moq;
using WorkLog.Application.Abstractions;
using WorkLog.Application.UseCases;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.Tests;

public class GetProjectsUseCaseTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 22, 1, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProjectRepository> _projectRepository = new();

    [Fact]
    public async Task ExecuteAsync_ShouldReturnProjectsFromRepository()
    {
        // Arrange
        var projects = new List<Project>
        {
            new("Project C", FixedNow, "3番目"),
            new("Project A", FixedNow, "1番目"),
            new("Project B", FixedNow, "2番目")
        };

        projects[0].Deactivate(FixedNow);

        _projectRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var useCase = new GetProjectsUseCase(
            _projectRepository.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Same(projects, result);

        // 無効化されたプロジェクトも含まれること
        Assert.False(result[0].IsActive);

        _projectRepository.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
