using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Options;
using Contoso.NotificationRelay.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contoso.NotificationRelay.Service;

/// <summary>
/// Core processing engine. Runs as a hosted BackgroundService.
/// Dependencies are provided via constructor injection.
/// </summary>
public class NotificationRelayEngine : BackgroundService
{
    private readonly IQueueConsumer _queueConsumer;
    private readonly NotificationDispatcher _dispatcher;
    private readonly ILogger<NotificationRelayEngine> _logger;
    private readonly int _pollIntervalMs;

    public NotificationRelayEngine(
        IQueueConsumer queueConsumer,
        NotificationDispatcher dispatcher,
        ILogger<NotificationRelayEngine> logger,
        IOptions<NotificationRelayOptions> options)
    {
        _queueConsumer = queueConsumer;
        _dispatcher = dispatcher;
        _logger = logger;
        _pollIntervalMs = options.Value.PollIntervalMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationRelayEngine started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = _queueConsumer.Dequeue();

                if (message is null)
                {
                    await Task.Delay(_pollIntervalMs, stoppingToken);
                    continue;
                }

                _logger.LogInformation(
                    "Processing {Type} message for {To} (MessageId={MessageId}, CorrelationId={CorrelationId}).",
                    message.Type, message.To, message.MessageId, message.CorrelationId);

                bool success = _dispatcher.Dispatch(message);

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
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("NotificationRelayEngine stopped.");
    }
}
