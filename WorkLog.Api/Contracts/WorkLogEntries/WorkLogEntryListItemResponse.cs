namespace WorkLog.Api.Contracts.WorkLogEntries;

public sealed record WorkLogEntryListItemResponse(
    Guid Id,
    DateOnly WorkDate,
    Guid ProjectId,
    string ProjectName,
    bool ProjectIsActive,
    string Description,
    int DurationMinutes);
