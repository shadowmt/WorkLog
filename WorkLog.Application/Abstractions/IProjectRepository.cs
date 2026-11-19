using WorkLog.Domain.Entities;

namespace WorkLog.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
