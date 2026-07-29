using System;
using Xunit;
using Contoso.NotificationRelay.Infrastructure.Deduplication;

namespace Contoso.NotificationRelay.UnitTests
{
    public class DeduplicationStoreTests
    {
        private readonly InMemoryDeduplicationStore _sut = new InMemoryDeduplicationStore();

        [Fact]
        public void HasBeenProcessed_ReturnsFalse_WhenMessageIsNew()
        {
            Guid id = Guid.NewGuid();
            Assert.False(_sut.HasBeenProcessed(id));
        }

        [Fact]
        public void HasBeenProcessed_ReturnsTrue_AfterMarkProcessed()
        {
            Guid id = Guid.NewGuid();
            _sut.MarkProcessed(id);
            Assert.True(_sut.HasBeenProcessed(id));
        }

        [Fact]
        public void MarkProcessed_IsIdempotent()
        {
            Guid id = Guid.NewGuid();
            _sut.MarkProcessed(id);
            _sut.MarkProcessed(id); // should not throw
            Assert.True(_sut.HasBeenProcessed(id));
        }

        [Fact]
        public void DifferentMessageIds_AreTrackedIndependently()
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            _sut.MarkProcessed(id1);

            Assert.True(_sut.HasBeenProcessed(id1));
            Assert.False(_sut.HasBeenProcessed(id2));
        }
    }
}
