using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkLog.Application.Abstractions;
using WorkLog.Infrastructure.Persistence;
using WorkLog.Infrastructure.Repositories;

namespace WorkLog.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "WorkLog";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // 接続文字列は DbContext の解決時に読み出す。
        // 登録時に文字列へ展開してしまうと、WebApplicationFactory が設定を差し替える前に
        // 値が確定し、テストが開発用 DB（User Secrets）へ接続してしまう。
        services.AddDbContext<WorkLogDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                serviceProvider
                    .GetRequiredService<IConfiguration>()
                    .GetRequiredWorkLogConnectionString()));

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWorkLogEntryRepository, WorkLogEntryRepository>();

        return services;
    }

    /// <summary>
    /// 接続文字列を返す。未設定の場合は InvalidOperationException を投げる。
    /// </summary>
    public static string GetRequiredWorkLogConnectionString(
        this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                $"The '{ConnectionStringName}' connection string is missing. Configure ConnectionStrings:{ConnectionStringName} before starting the API.")
            : connectionString;
    }
}
