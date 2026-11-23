using System.Text.Json;
using WorkLog.Application.Models;

namespace WorkLog.Api.Contracts.WorkLogEntries;

public static class UpdateWorkLogEntryRequestParser
{
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
        if (!json.TryGetProperty("workDate", out var value))
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
        if (!json.TryGetProperty("projectId", out var value))
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
        if (!json.TryGetProperty("description", out var value))
            return Optional<string>.Unspecified;

        if (value.ValueKind != JsonValueKind.String)
            throw new ArgumentException("descriptionは文字列である必要があります。");

        var description = value.GetString();

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("descriptionは空にできません。");

        if (description.Length > 2000)
            throw new ArgumentException("descriptionは2000文字以内にしてください。");

        return Optional<string>.Specified(description);
    }

    private static Optional<int> ParseDurationMinutes(JsonElement json)
    {
        if (!json.TryGetProperty("durationMinutes", out var value))
            return Optional<int>.Unspecified;

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var durationMinutes) ||
            durationMinutes is < 1 or > 1440)
        {
            throw new ArgumentException(
                "durationMinutesは1から1440までの整数である必要があります。");
        }

        return Optional<int>.Specified(durationMinutes);
    }
}
