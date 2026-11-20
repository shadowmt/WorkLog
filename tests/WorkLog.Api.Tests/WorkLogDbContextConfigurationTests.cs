using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkLog.Infrastructure.Persistence;

namespace WorkLog.Api.Tests;

/// <summary>
/// API ホストがテスト用 DB へ向いていることの検証。
/// Program.cs が接続文字列を起動時に文字列へ展開すると、WebApplicationFactory が
/// 設定を差し替える前に値が確定し、User Secrets の開発用 DB へ接続してしまう。
/// </summary>
public class WorkLogDbContextConfigurationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public void WorkLogDbContext_ShouldUseConnectionStringFromTestConfiguration()
    {
        // Act
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkLogDbContext>();

        // Assert
        Assert.Equal(
            CustomWebApplicationFactory.TestConnectionString,
            dbContext.Database.GetConnectionString());
    }
}
