using System.Threading.Tasks;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Service;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Contoso.NotificationRelay.IntegrationTests
{
    public class WorkerFlowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public WorkerFlowTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Worker_ProcessesSeededMessages_AcrossAllProviders()
        {
            // Starting a client boots the host, which starts the RelayWorker background service.
            _factory.CreateClient();

            var email = (InMemoryEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
            var sms = (InMemorySmsSender)_factory.Services.GetRequiredService<ISmsSender>();
            var teams = (InMemoryTeamsSender)_factory.Services.GetRequiredService<ITeamsSender>();

            // The worker seeds one message per provider and dispatches them on its first poll iterations.
            bool processed = await WaitUntilAsync(() =>
                email.GetSentMessages().Count == 1 &&
                sms.GetSentMessages().Count == 1 &&
                teams.GetSentMessages().Count == 1);

            Assert.True(processed,
                $"Expected 1 message per provider. Email={email.GetSentMessages().Count}, " +
                $"Sms={sms.GetSentMessages().Count}, Teams={teams.GetSentMessages().Count}");
        }

        private static async Task<bool> WaitUntilAsync(System.Func<bool> condition)
        {
            for (int i = 0; i < 50; i++)
            {
                if (condition()) return true;
                await Task.Delay(100);
            }
            return condition();
        }
    }
}
