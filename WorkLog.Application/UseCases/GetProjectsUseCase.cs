using WorkLog.Application.Abstractions;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.UseCases;

public sealed class GetProjectsUseCase(
    IProjectRepository projectRepository)
{
    public Task<IReadOnlyList<Project>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        return projectRepository.GetAllAsync(cancellationToken);
    }
}
