using WorkLog.Domain.Entities;

namespace WorkLog.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default);
}
