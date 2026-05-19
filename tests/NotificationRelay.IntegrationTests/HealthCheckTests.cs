using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Contoso.NotificationRelay.IntegrationTests
{
    public class HealthCheckTests : IClassFixture<WebApplicationFactory<Contoso.NotificationRelay.Service.Program>>
    {
        private readonly WebApplicationFactory<Contoso.NotificationRelay.Service.Program> _factory;

        public HealthCheckTests(WebApplicationFactory<Contoso.NotificationRelay.Service.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthEndpoint_ReturnsHealthy()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/healthz");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }
    }
}
