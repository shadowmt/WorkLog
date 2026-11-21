using WorkLog.Application.Models;
using WorkLog.Domain.Entities;

namespace WorkLog.Application.Abstractions;

public interface IWorkLogEntryRepository
{
    Task AddAsync(
        WorkLogEntry workLogEntry,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WorkLogEntryListItem>> GetPagedAsync(
        WorkLogEntrySearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
