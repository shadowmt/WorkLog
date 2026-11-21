namespace WorkLog.Application.Models;

public sealed record WorkLogEntrySearchCriteria(
    DateOnly? From,
    DateOnly? To,
    Guid? ProjectId,
    int Page,
    int PageSize);
