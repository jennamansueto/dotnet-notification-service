using System.Net;
using System.Threading.Tasks;
using Contoso.NotificationRelay.Service;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Contoso.NotificationRelay.IntegrationTests
{
    public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HealthCheckTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthEndpoint_ReturnsHealthy()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/healthz");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", body);
        }
    }
}
