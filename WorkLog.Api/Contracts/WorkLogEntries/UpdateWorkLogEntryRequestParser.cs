using System.Text.Json;
using WorkLog.Application.Models;

namespace WorkLog.Api.Contracts.WorkLogEntries;

public static class UpdateWorkLogEntryRequestParser
{
    /// <summary>durationMinutes の下限。OpenAPI スキーマと検証で共有する。</summary>
    public const int MinDurationMinutes = 1;

    /// <summary>durationMinutes の上限。OpenAPI スキーマと検証で共有する。</summary>
    public const int MaxDurationMinutes = 1440;

    /// <summary>description の文字数下限。クリアは null で表すため 1 文字以上。OpenAPI スキーマと検証で共有する。</summary>
    public const int MinDescriptionLength = 1;

    /// <summary>description の文字数上限。OpenAPI スキーマと検証で共有する。</summary>
    public const int MaxDescriptionLength = 2000;

    public static UpdateWorkLogEntryRequest Parse(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "リクエストのJSONはオブジェクトである必要があります。");
        }

        return new UpdateWorkLogEntryRequest(
            ParseWorkDate(json),
            ParseProjectId(json),
            ParseDescription(json),
            ParseDurationMinutes(json));
    }

    private static Optional<DateOnly> ParseWorkDate(JsonElement json)
    {
        if (!TryGetPropertyIgnoreCase(json, "workDate", out var value))
            return Optional<DateOnly>.Unspecified;

        if (value.ValueKind != JsonValueKind.String)
            throw new ArgumentException("workDateの形式が正しくありません。");

        try
        {
            var workDate = value.Deserialize<DateOnly>();

            return Optional<DateOnly>.Specified(workDate);
        }
        catch (JsonException)
        {
            throw new ArgumentException("workDateの日付が正しくありません。");
        }
    }

    private static Optional<Guid> ParseProjectId(JsonElement json)
    {
        if (!TryGetPropertyIgnoreCase(json, "projectId", out var value))
            return Optional<Guid>.Unspecified;

        if (value.ValueKind != JsonValueKind.String ||
            !value.TryGetGuid(out var projectId))
        {
            throw new ArgumentException("projectIdの形式が正しくありません。");
        }

        return Optional<Guid>.Specified(projectId);
    }

    private static Optional<string> ParseDescription(JsonElement json)
    {
        if (!TryGetPropertyIgnoreCase(json, "description", out var value))
            return Optional<string>.Unspecified;

        if (value.ValueKind != JsonValueKind.String)
            throw new ArgumentException("descriptionは文字列である必要があります。");

        var description = value.GetString();

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("descriptionは空にできません。");

        if (description.Length > MaxDescriptionLength)
            throw new ArgumentException($"descriptionは{MaxDescriptionLength}文字以内にしてください。");

        return Optional<string>.Specified(description);
    }

    private static Optional<int> ParseDurationMinutes(JsonElement json)
    {
        if (!TryGetPropertyIgnoreCase(json, "durationMinutes", out var value))
            return Optional<int>.Unspecified;

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var durationMinutes) ||
            durationMinutes is < MinDurationMinutes or > MaxDurationMinutes)
        {
            throw new ArgumentException(
                $"durationMinutesは{MinDurationMinutes}から{MaxDurationMinutes}までの整数である必要があります。");
        }

        return Optional<int>.Specified(durationMinutes);
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement json,
        string propertyName,
        out JsonElement value)
    {
        value = default;
        var found = false;

        foreach (var property in json.EnumerateObject())
        {
            if (string.Equals(
                property.Name,
                propertyName,
                StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                found = true;
            }
        }

        return found;
    }
}
