namespace Contoso.NotificationRelay.Domain.Models;

public record NotificationMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public NotificationType Type { get; init; }
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
