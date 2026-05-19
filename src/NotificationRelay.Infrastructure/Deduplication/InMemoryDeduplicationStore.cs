using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Infrastructure.Deduplication
{
    public class InMemoryDeduplicationStore : IDeduplicationStore
    {
        private readonly ConcurrentDictionary<Guid, byte> _processed = new ConcurrentDictionary<Guid, byte>();

        public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_processed.ContainsKey(messageId));
        }

        public Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            _processed.TryAdd(messageId, 0);
            return Task.CompletedTask;
        }
    }
}
