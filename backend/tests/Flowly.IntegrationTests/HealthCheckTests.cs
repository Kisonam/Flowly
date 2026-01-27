using System.Net;
using Flowly.IntegrationTests.Helpers;
using Xunit;
using FluentAssertions;

namespace Flowly.IntegrationTests;

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
        
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
