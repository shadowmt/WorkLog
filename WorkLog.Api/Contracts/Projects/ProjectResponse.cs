namespace WorkLog.Api.Contracts.Projects;

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);
