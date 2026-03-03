using System;

namespace Contoso.NotificationRelay.Domain.Models
{
    public class NotificationMessage
    {
        public Guid MessageId { get; set; }
        public NotificationType Type { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string CorrelationId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public NotificationMessage()
        {
            MessageId = Guid.NewGuid();
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
