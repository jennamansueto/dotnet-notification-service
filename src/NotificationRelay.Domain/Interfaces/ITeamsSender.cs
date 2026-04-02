using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Domain.Interfaces;

public interface ITeamsSender
{
    void Send(NotificationMessage message);
}
