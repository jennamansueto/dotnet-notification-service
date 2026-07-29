using System.Threading.Tasks;
using Contoso.NotificationRelay.Domain.Models;

namespace Contoso.NotificationRelay.Domain.Interfaces
{
    public interface ISmsSender
    {
        Task SendAsync(NotificationMessage message);
    }
}
