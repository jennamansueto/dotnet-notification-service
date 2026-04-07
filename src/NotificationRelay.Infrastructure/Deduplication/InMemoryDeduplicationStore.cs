using System.Collections.Concurrent;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Infrastructure.Deduplication;

public class InMemoryDeduplicationStore : IDeduplicationStore
{
    private readonly ConcurrentDictionary<Guid, byte> _processed = new();

    public bool HasBeenProcessed(Guid messageId)
    {
        return _processed.ContainsKey(messageId);
    }

    public void MarkProcessed(Guid messageId)
    {
        _processed.TryAdd(messageId, 0);
    }
}
