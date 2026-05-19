using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Infrastructure;
using Contoso.NotificationRelay.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RelaySettings>(builder.Configuration.GetSection("Relay"));

builder.Services.AddNotificationRelayInfrastructure();

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

namespace Contoso.NotificationRelay.Service
{
    public partial class Program { }
}
