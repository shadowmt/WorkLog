namespace WorkLog.Api.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void WebApplicationFactory_ShouldStartApi()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();

        // Act
        using var client = factory.CreateClient();

        // Assert
        Assert.NotNull(client);
    }
}
