using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WorkLog.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringEnvVar = "WORKLOG_TEST_CONNECTION_STRING";

    /// <summary>
    /// テスト用 PostgreSQL（compose.yaml の postgres-test）へ向けた接続文字列。
    /// TestDatabaseFixture と同じ規約で環境変数から読み、パスワードをソースに置かない。
    /// User Secrets の開発用 DB ではなくこちらへ接続されていることを、テスト側から検証するために公開する。
    /// </summary>
    public static string TestConnectionString
    {
        get
        {
            var connectionString =
                Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"{ConnectionStringEnvVar} が設定されていません。");
            }

            return connectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // 開発環境の設定や User Secrets を読み込まない
            configuration.Sources.Clear();

            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:WorkLog"] = TestConnectionString
                });
        });
    }
}
