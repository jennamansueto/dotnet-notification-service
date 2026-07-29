using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Infrastructure.Queue
{
    public class InMemoryQueueConsumer : IQueueConsumer
    {
        private readonly ConcurrentQueue<NotificationMessage> _queue = new ConcurrentQueue<NotificationMessage>();
        private readonly ILogger<InMemoryQueueConsumer> _logger;

        public InMemoryQueueConsumer(ILogger<InMemoryQueueConsumer> logger, bool seedSampleMessages = true)
        {
            _logger = logger;
            if (seedSampleMessages)
            {
                SeedSampleMessages();
            }
        }

        public Task<NotificationMessage> DequeueAsync(CancellationToken cancellationToken)
        {
            NotificationMessage message;
            if (_queue.TryDequeue(out message))
            {
                _logger.LogDebug("Dequeued MessageId={MessageId}.", message.MessageId);
                return Task.FromResult(message);
            }
            return Task.FromResult<NotificationMessage>(null);
        }

        public void Enqueue(NotificationMessage message)
        {
            _queue.Enqueue(message);
        }

        private void SeedSampleMessages()
        {
            string correlationId = Guid.NewGuid().ToString("N");

            var samples = new List<NotificationMessage>
            {
                new NotificationMessage
                {
                    MessageId = Guid.NewGuid(),
                    Type = NotificationType.Email,
                    To = "alice@contoso.com",
                    Subject = "Invoice #1042 Ready",
                    Body = "Your invoice is available for download.",
                    CorrelationId = correlationId
                },
                new NotificationMessage
                {
                    MessageId = Guid.NewGuid(),
                    Type = NotificationType.Sms,
                    To = "+15551234567",
                    Subject = "Appointment Reminder",
                    Body = "Your appointment is tomorrow at 10 AM.",
                    CorrelationId = correlationId
                },
                new NotificationMessage
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
}
