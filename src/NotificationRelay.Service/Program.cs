using Contoso.NotificationRelay.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contoso.NotificationRelay.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Enables running under the Windows Service Control Manager when installed
            // as a service (no-op on other platforms). Replaces the ServiceBase host.
            builder.Host.UseWindowsService();

            builder.Services.AddNotificationRelayServices(builder.Configuration);
            builder.Services.AddHostedService<RelayWorker>();
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            app.MapHealthChecks("/healthz");

            app.Run();
        }
    }
}
