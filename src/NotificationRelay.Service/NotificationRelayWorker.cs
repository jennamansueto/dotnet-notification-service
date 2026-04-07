using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Contoso.NotificationRelay.Service;

public class NotificationRelayWorker : BackgroundService
{
    private readonly ILogger<NotificationRelayWorker> _logger;
    private readonly NotificationRelayOptions _options;
    private readonly IQueueConsumer _queueConsumer;
    private readonly NotificationDispatcher _dispatcher;

    public NotificationRelayWorker(
        ILogger<NotificationRelayWorker> logger,
        IOptions<NotificationRelayOptions> options,
        IQueueConsumer queueConsumer,
        NotificationDispatcher dispatcher)
    {
        _logger = logger;
        _options = options.Value;
        _queueConsumer = queueConsumer;
        _dispatcher = dispatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationRelay worker starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = _queueConsumer.Dequeue();
                if (message != null)
                {
                    _dispatcher.Dispatch(message);
                }
                else
                {
                    await Task.Delay(_options.PollIntervalMs, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification.");
                await Task.Delay(_options.PollIntervalMs, stoppingToken);
            }
        }
        _logger.LogInformation("NotificationRelay worker stopped.");
    }
}
