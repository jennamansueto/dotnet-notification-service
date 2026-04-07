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

    public bool Dispatch(NotificationMessage message)
    {
        if (_deduplicationStore.HasBeenProcessed(message.MessageId))
        {
            _logger.LogInformation(
                "Duplicate detected. Skipping MessageId={MessageId} CorrelationId={CorrelationId}.",
                message.MessageId, message.CorrelationId);
            return true;
        }

        bool success = _retryPolicy.Execute(
            () => SendByType(message),
            $"Send{message.Type}",
            message.CorrelationId);

        if (success)
        {
            _deduplicationStore.MarkProcessed(message.MessageId);
            _logger.LogInformation(
                "Dispatched {Type} notification MessageId={MessageId} CorrelationId={CorrelationId}.",
                message.Type, message.MessageId, message.CorrelationId);
        }

        return success;
    }

    private void SendByType(NotificationMessage message)
    {
        switch (message.Type)
        {
            case NotificationType.Email:
                _emailSender.Send(message);
                break;
            case NotificationType.Sms:
                _smsSender.Send(message);
                break;
            case NotificationType.Teams:
                _teamsSender.Send(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message),
                    $"Unknown notification type: {message.Type}");
        }
    }
}
