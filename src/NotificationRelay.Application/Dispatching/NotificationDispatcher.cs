using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Application.Dispatching;

public class NotificationDispatcher
{
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly ITeamsSender _teamsSender;
    private readonly IDeduplicationStore _deduplicationStore;
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEmailSender emailSender,
        ISmsSender smsSender,
        ITeamsSender teamsSender,
        IDeduplicationStore deduplicationStore,
        RetryPolicy retryPolicy,
        ILogger<NotificationDispatcher> logger)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
        _teamsSender = teamsSender;
        _deduplicationStore = deduplicationStore;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task<bool> DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (_deduplicationStore.HasBeenProcessed(message.MessageId))
        {
            _logger.LogInformation(
                "Duplicate detected. Skipping MessageId={MessageId} CorrelationId={CorrelationId}.",
                message.MessageId, message.CorrelationId);
            return true;
        }

        bool success = await _retryPolicy.ExecuteAsync(
            () => SendByTypeAsync(message, cancellationToken),
            "Send" + message.Type,
            message.CorrelationId,
            cancellationToken);

        if (success)
        {
            _deduplicationStore.MarkProcessed(message.MessageId);
            _logger.LogInformation(
                "Dispatched {Type} notification MessageId={MessageId} CorrelationId={CorrelationId}.",
                message.Type, message.MessageId, message.CorrelationId);
        }

        return success;
    }

    private Task SendByTypeAsync(NotificationMessage message, CancellationToken cancellationToken) =>
        message.Type switch
        {
            NotificationType.Email => _emailSender.SendAsync(message, cancellationToken),
            NotificationType.Sms => _smsSender.SendAsync(message, cancellationToken),
            NotificationType.Teams => _teamsSender.SendAsync(message, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(message), $"Unknown notification type: {message.Type}")
        };
}
