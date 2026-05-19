using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Infrastructure.Providers;

public class InMemoryEmailSender : IEmailSender
{
    private readonly ILogger<InMemoryEmailSender> _logger;
    private readonly List<NotificationMessage> _sent = [];
    private readonly object _lock = new();

    public InMemoryEmailSender(ILogger<InMemoryEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        lock (_lock) { _sent.Add(message); }
        _logger.LogInformation(
            "[EMAIL SENT] To={To} Subject={Subject} MessageId={MessageId} CorrelationId={CorrelationId}",
            message.To, message.Subject, message.MessageId, message.CorrelationId);
        return Task.CompletedTask;
    }

    public IReadOnlyList<NotificationMessage> GetSentMessages()
    {
        lock (_lock) { return new List<NotificationMessage>(_sent).AsReadOnly(); }
    }
}
