using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Options;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Infrastructure.Queue;
using Contoso.NotificationRelay.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<NotificationRelayOptions>(
            ctx.Configuration.GetSection(NotificationRelayOptions.SectionName));

        services.AddSingleton<IEmailSender, InMemoryEmailSender>();
        services.AddSingleton<ISmsSender, InMemorySmsSender>();
        services.AddSingleton<ITeamsSender, InMemoryTeamsSender>();
        services.AddSingleton<IQueueConsumer, InMemoryQueueConsumer>();
        services.AddSingleton<IDeduplicationStore, InMemoryDeduplicationStore>();
        services.AddSingleton<RetryPolicy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NotificationRelayOptions>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RetryPolicy>>();
            return new RetryPolicy(logger, options.RetryCount, options.RetryBackoffMs);
        });
        services.AddSingleton<NotificationDispatcher>();
        services.AddHostedService<NotificationRelayEngine>();
    });

var host = builder.Build();
await host.RunAsync();
