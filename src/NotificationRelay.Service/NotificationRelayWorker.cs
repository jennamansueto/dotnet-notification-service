using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Service
{
    /// <summary>
    /// .NET 8 BackgroundService that replaces the legacy NotificationRelayWindowsService
    /// and the manual thread management in NotificationRelayEngine.
    /// The Generic Host handles both console and Windows Service modes automatically.
    /// </summary>
    public class NotificationRelayWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly NotificationRelayOptions _options;

        public NotificationRelayWorker(ILogger logger, IOptions<NotificationRelayOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("NotificationRelayWorker started.");

            try
            {
                using (var engine = new NotificationRelayEngine(_logger, _options))
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            engine.PollOnce();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(string.Format("Unhandled exception in poll loop: {0}", ex));
                            await Task.Delay(5000, stoppingToken);
                            continue;
                        }

                        await Task.Delay(_options.PollIntervalMs, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — host cancelled the token
            }

            _logger.Info("NotificationRelayWorker stopped.");
        }
    }
}
