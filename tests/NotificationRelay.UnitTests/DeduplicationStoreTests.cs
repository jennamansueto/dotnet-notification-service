using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contoso.NotificationRelay.Infrastructure.Deduplication;

namespace Contoso.NotificationRelay.UnitTests
{
    [TestClass]
    public class DeduplicationStoreTests
    {
        private readonly InMemoryDeduplicationStore _sut = new InMemoryDeduplicationStore();

        [TestMethod]
        public void HasBeenProcessed_ReturnsFalse_WhenMessageIsNew()
        {
            Guid id = Guid.NewGuid();
            Assert.IsFalse(_sut.HasBeenProcessed(id));
        }

        [TestMethod]
        public void HasBeenProcessed_ReturnsTrue_AfterMarkProcessed()
        {
            Guid id = Guid.NewGuid();
            _sut.MarkProcessed(id);
            Assert.IsTrue(_sut.HasBeenProcessed(id));
        }

        [TestMethod]
        public void MarkProcessed_IsIdempotent()
        {
            Guid id = Guid.NewGuid();
            _sut.MarkProcessed(id);
            _sut.MarkProcessed(id); // should not throw
            Assert.IsTrue(_sut.HasBeenProcessed(id));
        }

        [TestMethod]
        public void DifferentMessageIds_AreTrackedIndependently()
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            _sut.MarkProcessed(id1);

            Assert.IsTrue(_sut.HasBeenProcessed(id1));
            Assert.IsFalse(_sut.HasBeenProcessed(id2));
        }
    }
}
