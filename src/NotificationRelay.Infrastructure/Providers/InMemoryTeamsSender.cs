using System.Collections.Generic;
using System.Threading.Tasks;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Infrastructure.Providers
{
    public class InMemoryTeamsSender : ITeamsSender
    {
        private readonly ILogger<InMemoryTeamsSender> _logger;
        private readonly List<NotificationMessage> _sent = new List<NotificationMessage>();
        private readonly object _lock = new object();

        public InMemoryTeamsSender(ILogger<InMemoryTeamsSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(NotificationMessage message)
        {
            lock (_lock) { _sent.Add(message); }
            _logger.LogInformation(
                "[TEAMS SENT] To={To} Subject={Subject} MessageId={MessageId} CorrelationId={CorrelationId}",
                message.To, message.Subject, message.MessageId, message.CorrelationId);
            return Task.CompletedTask;
        }

        public IReadOnlyList<NotificationMessage> GetSentMessages()
        {
            lock (_lock) { return new List<NotificationMessage>(_sent).AsReadOnly(); }
        }
    }
}
