using WorkLog.Domain.Entities;

namespace WorkLog.Application.Abstractions;

public interface IWorkLogEntryRepository
{
    Task AddAsync(
        WorkLogEntry workLogEntry,
        CancellationToken cancellationToken = default);
}
