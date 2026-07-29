using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Configuration;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Infrastructure.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contoso.NotificationRelay.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all notification relay services: configuration, infrastructure
        /// adapters, retry policy, and the dispatcher. Replaces the legacy
        /// "poor man's DI" manual wiring in NotificationRelayEngine.
        /// </summary>
        public static IServiceCollection AddNotificationRelayServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RelaySettings>(configuration.GetSection("Relay"));

            services.AddSingleton<IQueueConsumer, InMemoryQueueConsumer>();
            services.AddSingleton<IEmailSender, InMemoryEmailSender>();
            services.AddSingleton<ISmsSender, InMemorySmsSender>();
            services.AddSingleton<ITeamsSender, InMemoryTeamsSender>();
            services.AddSingleton<IDeduplicationStore, InMemoryDeduplicationStore>();

            services.AddSingleton<RetryPolicy>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<RelaySettings>>().Value;
                return new RetryPolicy(
                    sp.GetRequiredService<ILogger<RetryPolicy>>(),
                    settings.RetryCount,
                    settings.RetryBackoffMs);
            });

            services.AddSingleton<NotificationDispatcher>();

            return services;
        }
    }
}
