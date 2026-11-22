
namespace WorkLog.Domain.Entities;

/// <summary>
/// 作業実績
/// </summary>
public sealed class WorkLogEntry
{
    public Guid Id { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public int DurationMinutes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core 用
    private WorkLogEntry()
    {
    }

    public WorkLogEntry(
        DateOnly workDate,
        Guid projectId,
        string description,
        int durationMinutes,
        DateTimeOffset now)
    {
        Validate(workDate, projectId, description, durationMinutes);
        var createdAt = ToStoredUtc(now);

        Id = Guid.NewGuid();
        WorkDate = workDate;
        ProjectId = projectId;
        Description = description.Trim();
        DurationMinutes = durationMinutes;
        CreatedAt = createdAt;
        UpdatedAt = CreatedAt;
    }

    public void Update(
        DateOnly workDate,
        Guid projectId,
        string description,
        int durationMinutes,
        DateTimeOffset now)
    {
        Validate(workDate, projectId, description, durationMinutes);
        var updatedAt = ToStoredUtc(now);

        WorkDate = workDate;
        ProjectId = projectId;
        Description = description.Trim();
        DurationMinutes = durationMinutes;
        UpdatedAt = updatedAt;
    }

    private static DateTimeOffset ToStoredUtc(DateTimeOffset value)
    {
        if (value == default)
        {
            throw new ArgumentException(
                "時刻を指定してください。",
                nameof(value));
        }

        return value.ToUniversalTime();
    }

    private static void Validate(
        DateOnly workDate,
        Guid projectId,
        string description,
        int durationMinutes)
    {
        if (workDate == default)
        {
            throw new ArgumentException(
                "作業日は必須です。",
                nameof(workDate));
        }

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException(
                "プロジェクトは必須です。",
                nameof(projectId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMinutes),
                "作業時間は1分以上にしてください。");
        }
    }
}
