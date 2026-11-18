
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Infrastructure.Tests;

public class DatabaseConnectionTests
{
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

        // 安全ガード：許可した接続先以外は拒否する
        Assert.Equal("localhost", builder.Host);
        Assert.Equal(5435, builder.Port);
        Assert.Equal("worklog_test", builder.Database);
        Assert.Equal("worklog_user", builder.Username);

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
}
