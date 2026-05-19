using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Domain.Interfaces;

public interface IEmailSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
