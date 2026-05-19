using System;
using System.Threading.Tasks;
using Xunit;
using Contoso.NotificationRelay.Infrastructure.Deduplication;

namespace Contoso.NotificationRelay.UnitTests
{
    public class DeduplicationStoreTests
    {
        private readonly InMemoryDeduplicationStore _sut = new InMemoryDeduplicationStore();

        [Fact]
        public async Task HasBeenProcessedAsync_ReturnsFalse_WhenMessageIsNew()
        {
            Guid id = Guid.NewGuid();
            Assert.False(await _sut.HasBeenProcessedAsync(id));
        }

        [Fact]
        public async Task HasBeenProcessedAsync_ReturnsTrue_AfterMarkProcessed()
        {
            Guid id = Guid.NewGuid();
            await _sut.MarkProcessedAsync(id);
            Assert.True(await _sut.HasBeenProcessedAsync(id));
        }

        [Fact]
        public async Task MarkProcessedAsync_IsIdempotent()
        {
            Guid id = Guid.NewGuid();
            await _sut.MarkProcessedAsync(id);
            await _sut.MarkProcessedAsync(id); // should not throw
            Assert.True(await _sut.HasBeenProcessedAsync(id));
        }

        [Fact]
        public async Task DifferentMessageIds_AreTrackedIndependently()
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            await _sut.MarkProcessedAsync(id1);

            Assert.True(await _sut.HasBeenProcessedAsync(id1));
            Assert.False(await _sut.HasBeenProcessedAsync(id2));
        }
    }
}
