using Microsoft.Extensions.DependencyInjection;
using WorkLog.Application.UseCases;

namespace WorkLog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateWorkLogEntryUseCase>();
        services.AddScoped<GetWorkLogEntriesUseCase>();
        services.AddScoped<UpdateWorkLogEntryUseCase>();
        services.AddScoped<CreateProjectUseCase>();
        services.AddScoped<GetProjectsUseCase>();

        return services;
    }
}
