namespace WorkLog.Api.Contracts.WorkLogEntries;

public sealed record WorkLogEntryResponse(
    Guid Id,
    DateOnly WorkDate,
    Guid ProjectId,
    string Description,
    int DurationMinutes);
