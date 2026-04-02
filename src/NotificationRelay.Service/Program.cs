using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Logging;
using Contoso.NotificationRelay.Service;

var builder = Host.CreateApplicationBuilder(args);

// Bind the "NotificationRelay" config section to strongly-typed options
builder.Services.Configure<NotificationRelayOptions>(
    builder.Configuration.GetSection("NotificationRelay"));

// Register the custom ILogger (kept per user request — not replacing with M.E.Logging)
var options = new NotificationRelayOptions();
builder.Configuration.GetSection("NotificationRelay").Bind(options);
builder.Services.AddSingleton<ILogger>(sp => new FileAndConsoleLogger(options.LogDirectory));

// Support running as a Windows Service (no-op on Linux / console mode)
builder.Services.AddWindowsService();

// Register the background worker
builder.Services.AddHostedService<NotificationRelayWorker>();

var host = builder.Build();
host.Run();
