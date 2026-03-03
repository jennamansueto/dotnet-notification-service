using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Domain.Interfaces
{
    public interface IQueueConsumer
    {
        NotificationMessage Dequeue();
    }
}
