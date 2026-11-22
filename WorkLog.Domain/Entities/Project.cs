
namespace WorkLog.Domain.Entities;

/// <summary>
/// プロジェクトマスタ
/// </summary>
public sealed class Project
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core 用
    private Project()
    {
    }

    public Project(
        string name,
        DateTimeOffset now,
        string? description = null)
    {
        var normalizedName = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);
        var createdAt = ToStoredUtc(now);

        Id = Guid.NewGuid();
        Name = normalizedName;
        NormalizedName = normalizedName.ToUpperInvariant();
        Description = normalizedDescription;

        IsActive = true;

        CreatedAt = createdAt;
        UpdatedAt = CreatedAt;
    }

    public void Update(
        string name,
        string? description,
        DateTimeOffset now)
    {
        var normalizedName = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);
        var updatedAt = ToStoredUtc(now);

        Name = normalizedName;
        NormalizedName = normalizedName.ToUpperInvariant();
        Description = normalizedDescription;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTimeOffset now)
    {
        var updatedAt = ToStoredUtc(now);

        IsActive = false;
        UpdatedAt = updatedAt;
    }

    public void Activate(DateTimeOffset now)
    {
        var updatedAt = ToStoredUtc(now);

        IsActive = true;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 保持する時刻は常に UTC に正規化する。
    /// </summary>
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

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return name.Trim();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}
