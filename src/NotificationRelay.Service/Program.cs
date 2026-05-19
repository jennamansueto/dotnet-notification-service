using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Infrastructure.Queue;
using Contoso.NotificationRelay.Service;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RelaySettings>(builder.Configuration.GetSection("Relay"));

builder.Services.AddSingleton<IQueueConsumer, InMemoryQueueConsumer>();
builder.Services.AddSingleton<IEmailSender, InMemoryEmailSender>();
builder.Services.AddSingleton<ISmsSender, InMemorySmsSender>();
builder.Services.AddSingleton<ITeamsSender, InMemoryTeamsSender>();
builder.Services.AddSingleton<IDeduplicationStore, InMemoryDeduplicationStore>();

builder.Services.AddSingleton<RetryPolicy>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RelaySettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<RetryPolicy>>();
    return new RetryPolicy(logger, settings.RetryCount, settings.RetryBackoffMs);
});

builder.Services.AddSingleton<NotificationDispatcher>();

builder.Services.AddHostedService<NotificationRelayWorker>();

builder.Services.AddHealthChecks();

builder.Host.UseWindowsService();
builder.Host.UseSystemd();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
