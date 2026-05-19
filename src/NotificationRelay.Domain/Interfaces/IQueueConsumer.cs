using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Domain.Interfaces;

public interface IQueueConsumer
{
    Task<NotificationMessage?> DequeueAsync(CancellationToken cancellationToken = default);
}
