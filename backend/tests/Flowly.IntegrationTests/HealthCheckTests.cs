using System.Net;
using Flowly.IntegrationTests.Helpers;
using Xunit;
using FluentAssertions;

namespace Flowly.IntegrationTests;

/// <summary>
/// Базовий тест для перевірки що API запускається.
/// </summary>
public class HealthCheckTests : IClassFixture<FlowlyWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(FlowlyWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
