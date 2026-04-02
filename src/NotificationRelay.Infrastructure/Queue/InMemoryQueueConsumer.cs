using System.Collections.Concurrent;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Infrastructure.Queue;

public class InMemoryQueueConsumer : IQueueConsumer
{
    private readonly ConcurrentQueue<NotificationMessage> _queue = new();
    private readonly ILogger<InMemoryQueueConsumer> _logger;

    public InMemoryQueueConsumer(ILogger<InMemoryQueueConsumer> logger, bool seedSampleMessages = true)
    {
        _logger = logger;
        if (seedSampleMessages)
        {
            SeedSampleMessages();
        }
    }

    public NotificationMessage? Dequeue()
    {
        if (_queue.TryDequeue(out var message))
        {
            _logger.LogDebug("Dequeued MessageId={MessageId}.", message.MessageId);
            return message;
        }
        return null;
    }

    public void Enqueue(NotificationMessage message)
    {
        _queue.Enqueue(message);
    }

    private void SeedSampleMessages()
    {
        var correlationId = Guid.NewGuid().ToString("N");

        var samples = new List<NotificationMessage>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                Type = NotificationType.Email,
                To = "alice@contoso.com",
                Subject = "Invoice #1042 Ready",
                Body = "Your invoice is available for download.",
                CorrelationId = correlationId
            },
            new()
            {
                MessageId = Guid.NewGuid(),
                Type = NotificationType.Sms,
                To = "+15551234567",
                Subject = "Appointment Reminder",
                Body = "Your appointment is tomorrow at 10 AM.",
                CorrelationId = correlationId
            },
            new()
            {
                MessageId = Guid.NewGuid(),
                Type = NotificationType.Teams,
                To = "ops-channel",
                Subject = "Deployment Complete",
                Body = "Release v2.4.1 deployed to production successfully.",
                CorrelationId = correlationId
            }
        };

        foreach (var msg in samples)
        {
            _queue.Enqueue(msg);
        }

        _logger.LogInformation("Seeded {Count} sample notification messages.", samples.Count);
    }
}
