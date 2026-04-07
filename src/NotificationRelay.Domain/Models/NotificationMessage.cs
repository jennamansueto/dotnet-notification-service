namespace Contoso.NotificationRelay.Domain.Models;

public class NotificationMessage
{
    public Guid MessageId { get; set; }
    public NotificationType Type { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public NotificationMessage()
    {
        MessageId = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
