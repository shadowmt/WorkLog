
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Infrastructure.Tests;

public class DatabaseConnectionTests
{
    // compose.yaml のデフォルト値と同じ。
    // 環境変数が未設定のローカル実行でも同じ安全ガードが働くようにする。
    private const string DefaultPort = "5435";
    private const string DefaultDatabase = "worklog_test";
    private const string DefaultUsername = "worklog_user";

    [Fact]
    public async Task CanConnectToTestDatabase()
    {
        // Arrange
        var connectionString =
            Environment.GetEnvironmentVariable(
                "WORKLOG_TEST_CONNECTION_STRING");

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "テスト用接続文字列が設定されていません。");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        // 安全ガード：ループバック上のテスト用データベース以外には接続しない。
        // 期待値は compose.yaml と同じ環境変数から導出し、二重定義を防ぐ。
        // Host だけは意図的に固定する（リモートの DB をテスト対象にしない）。
        Assert.Equal("localhost", builder.Host);
        Assert.Equal(ExpectedPort(), builder.Port);
        Assert.Equal(
            EnvOrDefault("WORKLOG_TEST_DB", DefaultDatabase),
            builder.Database);
        Assert.Equal(
            EnvOrDefault("WORKLOG_TEST_USER", DefaultUsername),
            builder.Username);

        var options = new DbContextOptionsBuilder<WorkLogDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;

        await using var dbContext = new WorkLogDbContext(options);

        // Act
        var canConnect = await dbContext.Database.CanConnectAsync();

        Assert.True(canConnect);

        // テストDBに未適用のマイグレーションを適用する
        await dbContext.Database.MigrateAsync();

        // Assert: マイグレーション後のテーブルにアクセスできること
        var projectsExist = await dbContext.Projects.AnyAsync();

        Assert.False(projectsExist);
    }

    private static string EnvOrDefault(string name, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static int ExpectedPort()
    {
        var raw = EnvOrDefault("WORKLOG_TEST_PORT", DefaultPort);

        Assert.True(
            int.TryParse(raw, out var port),
            $"WORKLOG_TEST_PORT が数値ではありません: {raw}");

        return port;
    }
}
