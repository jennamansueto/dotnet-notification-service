using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Infrastructure.Queue
{
    public class InMemoryQueueConsumer : IQueueConsumer
    {
        private readonly Channel<NotificationMessage> _channel = Channel.CreateUnbounded<NotificationMessage>();
        private readonly ILogger<InMemoryQueueConsumer> _logger;

        public InMemoryQueueConsumer(ILogger<InMemoryQueueConsumer> logger, bool seedSampleMessages = true)
        {
            _logger = logger;
            if (seedSampleMessages)
            {
                SeedSampleMessages();
            }
        }

        public Task<NotificationMessage> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (_channel.Reader.TryRead(out var message))
            {
                _logger.LogDebug("Dequeued MessageId={MessageId}.", message.MessageId);
                return Task.FromResult(message);
            }
            return Task.FromResult<NotificationMessage>(null);
        }

        public async Task EnqueueAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
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
                _channel.Writer.TryWrite(msg);
            }

            _logger.LogInformation("Seeded {Count} sample notification messages.", samples.Count);
        }
    }
}
