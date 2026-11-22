using Microsoft.Extensions.Time.Testing;
using Moq;
using WorkLog.Application.Abstractions;
using WorkLog.Application.Exceptions;
using WorkLog.Application.UseCases;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.Tests;

public class CreateProjectUseCaseTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 22, 1, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProjectRepository> _projectRepository = new();

    [Fact]
    public async Task ExecuteAsync_ShouldRegisterProject()
    {
        // Arrange
        var useCase = CreateUseCase();

        const string name = "  Test Project  ";
        const string description = "  プロジェクト登録テスト  ";

        // Act
        var result = await useCase.ExecuteAsync(
            name,
            description);

        // Assert
        Assert.Equal("Test Project", result.Name);
        Assert.Equal("TEST PROJECT", result.NormalizedName);
        Assert.Equal("プロジェクト登録テスト", result.Description);
        Assert.True(result.IsActive);
        Assert.Equal(FixedNow, result.CreatedAt);
        Assert.Equal(FixedNow, result.UpdatedAt);

        _projectRepository.Verify(
            x => x.AddAsync(
                It.Is<Project>(project =>
                    project.Id == result.Id &&
                    project.Name == "Test Project"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private CreateProjectUseCase CreateUseCase()
        => new(
            _projectRepository.Object,
            new FakeTimeProvider(FixedNow));

    [Fact]
    public async Task ExecuteAsync_ShouldThrowDuplicateProjectNameException_WhenProjectNameAlreadyExists()
    {
        // Arrange
        const string name = "Test Project";

        _projectRepository
            .Setup(x => x.ExistsByNormalizedNameAsync(
                "TEST PROJECT",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();

        // Act
        var act = () => useCase.ExecuteAsync(name, null);

        // Assert
        await Assert.ThrowsAsync<DuplicateProjectNameException>(act);

        _projectRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Project>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
