using System;
using System.Threading;
using System.Threading.Tasks;

namespace Contoso.NotificationRelay.Domain.Interfaces
{
    public interface IDeduplicationStore
    {
        Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
        Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}
