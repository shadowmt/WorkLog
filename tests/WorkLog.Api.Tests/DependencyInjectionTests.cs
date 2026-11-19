using Microsoft.Extensions.DependencyInjection;
using WorkLog.Application;
using WorkLog.Application.UseCases;
using WorkLog.Infrastructure;

namespace WorkLog.Api.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationAndInfrastructure_ShouldResolveCreateWorkLogEntryUseCase()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInfrastructure(
            "Host=localhost;Port=5435;Database=worklog_test;Username=worklog_user;Password=worklog_test_password");

        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var useCase = serviceProvider.GetRequiredService<CreateWorkLogEntryUseCase>();

        // Assert
        Assert.NotNull(useCase);
    }
}
