namespace WorkLog.Api.Contracts.WorkLogEntries;

public sealed record PagedWorkLogEntryResponse(
    IReadOnlyList<WorkLogEntryListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
