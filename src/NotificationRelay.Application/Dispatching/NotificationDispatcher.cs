using System;
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
        private readonly ILogger _logger;

        public NotificationDispatcher(
            IEmailSender emailSender,
            ISmsSender smsSender,
            ITeamsSender teamsSender,
            IDeduplicationStore deduplicationStore,
            RetryPolicy retryPolicy,
            ILogger logger)
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
                _logger.Info(string.Format(
                    "Duplicate detected. Skipping MessageId={0} CorrelationId={1}.",
                    message.MessageId, message.CorrelationId));
                return true;
            }

            bool success = _retryPolicy.Execute(
                () => SendByType(message),
                "Send" + message.Type,
                message.CorrelationId);

            if (success)
            {
                _deduplicationStore.MarkProcessed(message.MessageId);
                _logger.Info(string.Format(
                    "Dispatched {0} notification MessageId={1} CorrelationId={2}.",
                    message.Type, message.MessageId, message.CorrelationId));
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
                    throw new ArgumentOutOfRangeException("message",
                        string.Format("Unknown notification type: {0}", message.Type));
            }
        }
    }
}
