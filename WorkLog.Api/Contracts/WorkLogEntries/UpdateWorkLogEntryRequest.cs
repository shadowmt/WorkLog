using WorkLog.Application.Models;

namespace WorkLog.Api.Contracts.WorkLogEntries;

public sealed record UpdateWorkLogEntryRequest(
    Optional<DateOnly> WorkDate,
    Optional<Guid> ProjectId,
    Optional<string> Description,
    Optional<int> DurationMinutes);
