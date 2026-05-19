using Microsoft.Extensions.DependencyInjection;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Infrastructure.Queue;

namespace Contoso.NotificationRelay.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationRelayInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IQueueConsumer, InMemoryQueueConsumer>();
            services.AddSingleton<IEmailSender, InMemoryEmailSender>();
            services.AddSingleton<ISmsSender, InMemorySmsSender>();
            services.AddSingleton<ITeamsSender, InMemoryTeamsSender>();
            services.AddSingleton<IDeduplicationStore, InMemoryDeduplicationStore>();

            return services;
        }
    }
}
