namespace WorkLog.Application.Models;

public sealed record WorkLogEntryListItem(
    Guid Id,
    DateOnly WorkDate,
    Guid ProjectId,
    string ProjectName,
    bool ProjectIsActive,
    string Description,
    int DurationMinutes);
