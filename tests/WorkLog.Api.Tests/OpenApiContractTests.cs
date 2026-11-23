using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WorkLog.Api.Contracts.WorkLogEntries;

namespace WorkLog.Api.Tests;

/// <summary>
/// PATCH のリクエスト契約が OpenAPI に正しく現れているかの検証。
/// エンドポイントはボディを JsonElement で受け取るため、スキーマは
/// UpdateWorkLogEntryRequestBodyTransformer が置き換えている。
/// ルートやメソッドが変わってトランスフォーマーが外れたら、このテストが失敗する。
/// </summary>
public class OpenApiContractTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string PatchPath = "/api/work-log-entries/{id}";

    private WebApplicationFactory<Program> CreateDevelopmentFactory() =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    private static async Task<JsonElement> GetOpenApiDocumentAsync(
        WebApplicationFactory<Program> webFactory)
    {
        using var client = webFactory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        // JsonDocument は破棄すると子要素も使えなくなるため、必要な部分だけ複製して返す。
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldExposeUpdatableFields_InsteadOfJsonElement()
    {
        using var webFactory = CreateDevelopmentFactory();
        var root = await GetOpenApiDocumentAsync(webFactory);

        var patch = root
            .GetProperty("paths")
            .GetProperty(PatchPath)
            .GetProperty("patch");

        var schema = patch
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");

        Assert.False(schema.TryGetProperty("$ref", out _));
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");
        var names = properties.EnumerateObject()
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["description", "durationMinutes", "projectId", "workDate"],
            names);

        // 省略可能なフィールドなので required には立てられない。
        Assert.False(schema.TryGetProperty("required", out _));
    }

    [Fact]
    public async Task PatchWorkLogEntry_SchemaConstraints_ShouldMatchParserValidation()
    {
        using var webFactory = CreateDevelopmentFactory();
        var root = await GetOpenApiDocumentAsync(webFactory);

        var properties = root
            .GetProperty("paths")
            .GetProperty(PatchPath)
            .GetProperty("patch")
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("properties");

        Assert.Equal("string", properties.GetProperty("workDate").GetProperty("type").GetString());
        Assert.Equal("date", properties.GetProperty("workDate").GetProperty("format").GetString());

        Assert.Equal("string", properties.GetProperty("projectId").GetProperty("type").GetString());
        Assert.Equal("uuid", properties.GetProperty("projectId").GetProperty("format").GetString());

        var description = properties.GetProperty("description");
        Assert.Equal("string", description.GetProperty("type").GetString());
        Assert.Equal(
            UpdateWorkLogEntryRequestParser.MinDescriptionLength,
            description.GetProperty("minLength").GetInt32());
        Assert.Equal(
            UpdateWorkLogEntryRequestParser.MaxDescriptionLength,
            description.GetProperty("maxLength").GetInt32());

        var durationMinutes = properties.GetProperty("durationMinutes");
        Assert.Equal("integer", durationMinutes.GetProperty("type").GetString());
        Assert.Equal("int32", durationMinutes.GetProperty("format").GetString());
        Assert.Equal(
            UpdateWorkLogEntryRequestParser.MinDurationMinutes,
            durationMinutes.GetProperty("minimum").GetInt32());
        Assert.Equal(
            UpdateWorkLogEntryRequestParser.MaxDurationMinutes,
            durationMinutes.GetProperty("maximum").GetInt32());
    }

    [Fact]
    public async Task PatchWorkLogEntry_ShouldDeclareSuccessAndErrorResponses()
    {
        using var webFactory = CreateDevelopmentFactory();
        var root = await GetOpenApiDocumentAsync(webFactory);

        var responses = root
            .GetProperty("paths")
            .GetProperty(PatchPath)
            .GetProperty("patch")
            .GetProperty("responses");

        var statusCodes = responses.EnumerateObject()
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["200", "400", "404"], statusCodes);
    }
}
