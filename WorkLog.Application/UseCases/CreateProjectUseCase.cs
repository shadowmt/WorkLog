using WorkLog.Application.Abstractions;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.UseCases;

public sealed class CreateProjectUseCase(
    IProjectRepository projectRepository,
    TimeProvider timeProvider)
{
    public async Task<Project> ExecuteAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var project = new Project(
            name,
            timeProvider.GetUtcNow(),
            description);

        var exists = await projectRepository.ExistsByNormalizedNameAsync(
            project.NormalizedName,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "同じ名前のプロジェクトが既に存在します。");
        }

        await projectRepository.AddAsync(
            project,
            cancellationToken);

        return project;
    }
}
