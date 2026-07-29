using System;
using System.Threading;
using System.Threading.Tasks;
using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contoso.NotificationRelay.Service
{
    /// <summary>
    /// Background worker replacing the legacy NotificationRelayEngine poll loop.
    /// The stoppingToken drives graceful shutdown in place of the manual _running flag.
    /// </summary>
    public class RelayWorker : BackgroundService
    {
        private readonly IQueueConsumer _queueConsumer;
        private readonly NotificationDispatcher _dispatcher;
        private readonly ILogger<RelayWorker> _logger;
        private readonly int _pollIntervalMs;

        public RelayWorker(
            IQueueConsumer queueConsumer,
            NotificationDispatcher dispatcher,
            IOptions<RelaySettings> options,
            ILogger<RelayWorker> logger)
        {
            _queueConsumer = queueConsumer;
            _dispatcher = dispatcher;
            _logger = logger;
            _pollIntervalMs = options.Value.PollIntervalMs;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationRelay worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queueConsumer.DequeueAsync(stoppingToken).ConfigureAwait(false);

                    if (message == null)
                    {
                        await Task.Delay(_pollIntervalMs, stoppingToken).ConfigureAwait(false);
                        continue;
                    }

                    _logger.LogInformation(
                        "Processing {Type} message for {To} (MessageId={MessageId}, CorrelationId={CorrelationId}).",
                        message.Type, message.To, message.MessageId, message.CorrelationId);

                    bool success = await _dispatcher.DispatchAsync(message, stoppingToken).ConfigureAwait(false);

                    if (!success)
                    {
                        _logger.LogError(
                            "Failed to dispatch MessageId={MessageId} after retries.",
                            message.MessageId);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in poll loop.");
                    await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("NotificationRelay worker stopped.");
        }
    }
}
