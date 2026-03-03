using System.Collections.Generic;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Infrastructure.Providers
{
    public class InMemoryEmailSender : IEmailSender
    {
        private readonly ILogger _logger;
        private readonly List<NotificationMessage> _sent = new List<NotificationMessage>();
        private readonly object _lock = new object();

        public InMemoryEmailSender(ILogger logger)
        {
            _logger = logger;
        }

        public void Send(NotificationMessage message)
        {
            lock (_lock) { _sent.Add(message); }
            _logger.Info(string.Format(
                "[EMAIL SENT] To={0} Subject={1} MessageId={2} CorrelationId={3}",
                message.To, message.Subject, message.MessageId, message.CorrelationId));
        }

        public IReadOnlyList<NotificationMessage> GetSentMessages()
        {
            lock (_lock) { return new List<NotificationMessage>(_sent).AsReadOnly(); }
        }
    }
}
