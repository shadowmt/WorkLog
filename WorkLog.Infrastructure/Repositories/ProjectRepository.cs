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

    public async Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Projects.AddAsync(
            project,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .AnyAsync(
                project => project.NormalizedName == normalizedName,
                cancellationToken);
    }
}
