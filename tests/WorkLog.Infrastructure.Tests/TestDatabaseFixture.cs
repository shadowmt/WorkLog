using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Infrastructure.Tests;

public sealed class TestDatabaseFixture : IAsyncLifetime
{
    private const string DefaultPort = "5435";
    private const string DefaultDatabase = "worklog_test";
    private const string DefaultUsername = "worklog_user";

    private readonly DbContextOptions<WorkLogDbContext> _options;

    public TestDatabaseFixture()
    {
        ConnectionString = ReadConnectionString();

        var builder = Validate(ConnectionString);

        _options = new DbContextOptionsBuilder<WorkLogDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;
    }

    /// <summary>安全ガードを通過済みのテスト用接続文字列。</summary>
    public string ConnectionString { get; }

    public WorkLogDbContext CreateDbContext()
    {
        return new WorkLogDbContext(_options);
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        // テストDBに未適用のマイグレーションを適用する
        await dbContext.Database.MigrateAsync();
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static string ReadConnectionString()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "WORKLOG_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "テスト用接続文字列が設定されていません。");
        }

        return connectionString;
    }

    // 安全ガード：ループバック上のテスト用データベース以外には接続しない。
    // 期待値は compose.yaml と同じ環境変数から導出し、二重定義を防ぐ。
    // Host だけは意図的に固定する（リモートの DB をテスト対象にしない）。
    internal static NpgsqlConnectionStringBuilder Validate(
        string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var expectedPort = ExpectedPort();
        var expectedDatabase = EnvOrDefault("WORKLOG_TEST_DB", DefaultDatabase);
        var expectedUsername = EnvOrDefault("WORKLOG_TEST_USER", DefaultUsername);

        if (builder.Host != "localhost" ||
            builder.Port != expectedPort ||
            builder.Database != expectedDatabase ||
            builder.Username != expectedUsername)
        {
            // パスワードは意図的にメッセージへ含めない。
            throw new InvalidOperationException(
                "接続先が許可されたテストデータベースではありません。 " +
                $"接続先: Host={builder.Host}, Port={builder.Port}, " +
                $"Database={builder.Database}, Username={builder.Username} / " +
                $"期待値: Host=localhost, Port={expectedPort}, " +
                $"Database={expectedDatabase}, Username={expectedUsername}");
        }

        return builder;
    }

    private static string EnvOrDefault(string name, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static int ExpectedPort()
    {
        var raw = EnvOrDefault("WORKLOG_TEST_PORT", DefaultPort);

        if (!int.TryParse(raw, out var port))
        {
            throw new InvalidOperationException(
                $"WORKLOG_TEST_PORT が数値ではありません: {raw}");
        }

        return port;
    }
}