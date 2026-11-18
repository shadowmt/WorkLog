
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WorkLog.Infrastructure.Tests;

public class DatabaseConnectionTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task CanConnectToTestDatabase()
    {
        // Arrange
        // fixture の構築時点で安全ガードが検証済みのため、ここでは接続可否だけを見る。
        await using var dbContext = fixture.CreateDbContext();

        // Act
        var canConnect = await dbContext.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public void Guard_ShouldRejectNonLoopbackHost()
    {
        var connectionString =
            ConnectionStringWith(b => b.Host = "db.internal.example");

        Assert.Throws<InvalidOperationException>(
            () => TestDatabaseFixture.Validate(connectionString));
    }

    [Fact]
    public void Guard_ShouldRejectUnexpectedPort()
    {
        var connectionString =
            ConnectionStringWith(b => b.Port = b.Port == 59999 ? 59998 : 59999);

        Assert.Throws<InvalidOperationException>(
            () => TestDatabaseFixture.Validate(connectionString));
    }

    [Fact]
    public void Guard_ShouldRejectUnexpectedDatabase()
    {
        var connectionString =
            ConnectionStringWith(b => b.Database = "worklog_production");

        Assert.Throws<InvalidOperationException>(
            () => TestDatabaseFixture.Validate(connectionString));
    }

    [Fact]
    public void Guard_ShouldRejectUnexpectedUsername()
    {
        var connectionString =
            ConnectionStringWith(b => b.Username = "postgres");

        Assert.Throws<InvalidOperationException>(
            () => TestDatabaseFixture.Validate(connectionString));
    }

    // 許可された接続文字列を複製し、1項目だけ意図的にずらす。
    private string ConnectionStringWith(
        Action<NpgsqlConnectionStringBuilder> mutate)
    {
        var builder =
            new NpgsqlConnectionStringBuilder(fixture.ConnectionString);

        mutate(builder);

        return builder.ConnectionString;
    }
}
