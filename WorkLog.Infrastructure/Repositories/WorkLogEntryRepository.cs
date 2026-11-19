using WorkLog.Application.Abstractions;
using WorkLog.Domain.Entities;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Infrastructure.Repositories;

public sealed class WorkLogEntryRepository(
    WorkLogDbContext dbContext) : IWorkLogEntryRepository
{
    public async Task AddAsync(
        WorkLogEntry workLogEntry,
        CancellationToken cancellationToken = default)
    {
        await dbContext.WorkLogEntries.AddAsync(
            workLogEntry,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
