using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkLog.Application.Abstractions;
using WorkLog.Infrastructure.Persistence;
using WorkLog.Infrastructure.Repositories;

namespace WorkLog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<WorkLogDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWorkLogEntryRepository, WorkLogEntryRepository>();

        return services;
    }
}
