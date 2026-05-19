using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Service
{
    public class NotificationRelayWorker : BackgroundService
    {
        private readonly IQueueConsumer _queueConsumer;
        private readonly NotificationDispatcher _dispatcher;
        private readonly ILogger<NotificationRelayWorker> _logger;
        private readonly RelaySettings _settings;

        public NotificationRelayWorker(
            IQueueConsumer queueConsumer,
            NotificationDispatcher dispatcher,
            ILogger<NotificationRelayWorker> logger,
            IOptions<RelaySettings> settings)
        {
            _queueConsumer = queueConsumer;
            _dispatcher = dispatcher;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationRelayWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queueConsumer.DequeueAsync(stoppingToken).ConfigureAwait(false);

                    if (message == null)
                    {
                        await Task.Delay(_settings.PollIntervalMs, stoppingToken).ConfigureAwait(false);
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
                    try { await Task.Delay(5000, stoppingToken).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                }
            }

            _logger.LogInformation("NotificationRelayWorker stopped.");
        }
    }
}
