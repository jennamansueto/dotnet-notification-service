using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;

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

        public async Task<bool> DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            if (await _deduplicationStore.HasBeenProcessedAsync(message.MessageId, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation(
                    "Duplicate detected. Skipping MessageId={MessageId} CorrelationId={CorrelationId}.",
                    message.MessageId, message.CorrelationId);
                return true;
            }

            bool success = await _retryPolicy.ExecuteAsync(
                ct => SendByTypeAsync(message, ct),
                "Send" + message.Type,
                message.CorrelationId,
                cancellationToken).ConfigureAwait(false);

            if (success)
            {
                await _deduplicationStore.MarkProcessedAsync(message.MessageId, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Dispatched {Type} notification MessageId={MessageId} CorrelationId={CorrelationId}.",
                    message.Type, message.MessageId, message.CorrelationId);
            }

            return success;
        }

        private async Task SendByTypeAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            switch (message.Type)
            {
                case NotificationType.Email:
                    await _emailSender.SendAsync(message, cancellationToken).ConfigureAwait(false);
                    break;
                case NotificationType.Sms:
                    await _smsSender.SendAsync(message, cancellationToken).ConfigureAwait(false);
                    break;
                case NotificationType.Teams:
                    await _teamsSender.SendAsync(message, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message),
                        string.Format("Unknown notification type: {0}", message.Type));
            }
        }
    }
}
