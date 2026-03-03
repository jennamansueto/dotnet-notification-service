using System;

namespace Contoso.NotificationRelay.Domain.Interfaces
{
    public interface IDeduplicationStore
    {
        bool HasBeenProcessed(Guid messageId);
        void MarkProcessed(Guid messageId);
    }
}
