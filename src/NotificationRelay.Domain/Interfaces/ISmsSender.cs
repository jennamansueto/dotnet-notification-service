using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Domain.Interfaces
{
    public interface ISmsSender
    {
        void Send(NotificationMessage message);
    }
}
