using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Infrastructure.Queue;
using Contoso.NotificationRelay.Service;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((context, services) =>
    {
        services.Configure<NotificationRelayOptions>(
            context.Configuration.GetSection(NotificationRelayOptions.SectionName));

        var options = context.Configuration
            .GetSection(NotificationRelayOptions.SectionName)
            .Get<NotificationRelayOptions>() ?? new NotificationRelayOptions();

        services.AddSingleton<IQueueConsumer, InMemoryQueueConsumer>();
        services.AddSingleton<IEmailSender, InMemoryEmailSender>();
        services.AddSingleton<ISmsSender, InMemorySmsSender>();
        services.AddSingleton<ITeamsSender, InMemoryTeamsSender>();
        services.AddSingleton<IDeduplicationStore, InMemoryDeduplicationStore>();
        services.AddSingleton(sp =>
            new RetryPolicy(
                sp.GetRequiredService<ILogger<RetryPolicy>>(),
                options.RetryCount,
                options.RetryBackoffMs));
        services.AddSingleton<NotificationDispatcher>();

        services.AddHostedService<NotificationRelayWorker>();
    });

var host = builder.Build();
host.Run();
