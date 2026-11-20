using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WorkLog.Api.Tests;

/// <summary>
/// 接続文字列を一切設定せずに API を起動しようとするファクトリ。
/// 接続文字列は DbContext の解決時に読み出すため、Program.cs が起動時に検証していることを
/// ここで確認する（検証を外すと、設定不備は初回リクエストまで表面化しない）。
/// </summary>
public sealed class MissingConnectionStringFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.Sources.Clear());
    }
}

public class StartupValidationTests
{
    [Fact]
    public void CreateClient_ShouldThrow_WhenConnectionStringIsMissing()
    {
        // Arrange
        using var factory = new MissingConnectionStringFactory();

        // Act
        var exception = Record.Exception(() => factory.CreateClient());

        // Assert
        Assert.NotNull(exception);
        Assert.Contains("connection string is missing", exception!.ToString());
    }
}
