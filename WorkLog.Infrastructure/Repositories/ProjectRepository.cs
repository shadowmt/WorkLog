using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkLog.Application.Abstractions;
using WorkLog.Application.Exceptions;
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

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "ux_projects_name_normalized"
            })
        {
            throw new DuplicateProjectNameException();
        }
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

    public async Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
    }
}
