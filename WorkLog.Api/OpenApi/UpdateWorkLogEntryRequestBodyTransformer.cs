using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WorkLog.Api.Contracts.WorkLogEntries;
using WorkLog.Api.Endpoints;

namespace WorkLog.Api.OpenApi;

/// <summary>
/// PATCH /api/work-log-entries/{id} の requestBody スキーマを、実際の契約に置き換える。
///
/// このエンドポイントは部分更新を表現するためボディを <see cref="System.Text.Json.JsonElement"/>
/// として受け取り、<see cref="UpdateWorkLogEntryRequestParser"/> で Optional に変換する。
/// バインド型からスキーマを推論させると JsonElement の参照になり、契約が表現されない。
/// <c>WithOpenApi(Func&lt;OpenApiOperation, OpenApiOperation&gt;)</c> は .NET 10 で非推奨で、
/// requestBody の変更は無視されるため、サポートされているオペラショントランスフォーマーを使う。
/// </summary>
internal sealed class UpdateWorkLogEntryRequestBodyTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(context.Description.HttpMethod, HttpMethods.Patch, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        // RelativePath は先頭スラッシュの有無が実装依存なので、揃えて比較する。
        var expectedPath = WorkLogEntryEndpoints.UpdateRouteTemplate.TrimStart('/');
        var actualPath = context.Description.RelativePath?.TrimStart('/');

        if (!string.Equals(actualPath, expectedPath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        // 制約は検証の正であるパーサの定数から取る。ここで値を直書きしないこと。
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Description = "更新したいフィールドのみを指定する。省略したフィールドは変更されない。",
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["workDate"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "date",
                            },
                            ["projectId"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "uuid",
                            },
                            ["description"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                MinLength = UpdateWorkLogEntryRequestParser.MinDescriptionLength,
                                MaxLength = UpdateWorkLogEntryRequestParser.MaxDescriptionLength,
                            },
                            ["durationMinutes"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Integer,
                                Format = "int32",
                                Minimum = UpdateWorkLogEntryRequestParser.MinDurationMinutes.ToString(),
                                Maximum = UpdateWorkLogEntryRequestParser.MaxDurationMinutes.ToString(),
                            },
                        },
                    },
                },
            },
        };

        return Task.CompletedTask;
    }
}
