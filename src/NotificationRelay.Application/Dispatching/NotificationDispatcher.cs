using System;
using System.Threading;
using System.Threading.Tasks;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Application.Dispatching
{
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

        public async Task<bool> DispatchAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            if (_deduplicationStore.HasBeenProcessed(message.MessageId))
            {
                _logger.LogInformation(
                    "Duplicate detected. Skipping MessageId={MessageId} CorrelationId={CorrelationId}.",
                    message.MessageId, message.CorrelationId);
                return true;
            }

            bool success = await _retryPolicy.ExecuteAsync(
                () => SendByTypeAsync(message),
                "Send" + message.Type,
                message.CorrelationId,
                cancellationToken).ConfigureAwait(false);

            if (success)
            {
                _deduplicationStore.MarkProcessed(message.MessageId);
                _logger.LogInformation(
                    "Dispatched {Type} notification MessageId={MessageId} CorrelationId={CorrelationId}.",
                    message.Type, message.MessageId, message.CorrelationId);
            }

            return success;
        }

        private Task SendByTypeAsync(NotificationMessage message)
        {
            switch (message.Type)
            {
                case NotificationType.Email:
                    return _emailSender.SendAsync(message);
                case NotificationType.Sms:
                    return _smsSender.SendAsync(message);
                case NotificationType.Teams:
                    return _teamsSender.SendAsync(message);
                default:
                    throw new ArgumentOutOfRangeException("message",
                        string.Format("Unknown notification type: {0}", message.Type));
            }
        }
    }
}
