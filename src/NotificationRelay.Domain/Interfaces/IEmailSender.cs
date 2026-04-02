using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Domain.Interfaces;

public interface IEmailSender
{
    void Send(NotificationMessage message);
}
