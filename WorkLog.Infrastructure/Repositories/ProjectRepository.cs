using Microsoft.EntityFrameworkCore;
using WorkLog.Application.Abstractions;
using WorkLog.Domain.Entities;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Infrastructure.Repositories;

public sealed class ProjectRepository(
    WorkLogDbContext dbContext) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .FirstOrDefaultAsync(
                project => project.Id == id,
                cancellationToken);
    }
}
