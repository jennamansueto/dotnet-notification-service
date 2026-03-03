using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Models;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Logging;
using Contoso.NotificationRelay.Infrastructure.Providers;

namespace Contoso.NotificationRelay.UnitTests
{
    [TestClass]
    public class NotificationDispatcherTests
    {
        private InMemoryEmailSender _emailSender;
        private InMemorySmsSender _smsSender;
        private InMemoryTeamsSender _teamsSender;
        private InMemoryDeduplicationStore _dedupeStore;
        private NotificationDispatcher _sut;

        [TestInitialize]
        public void SetUp()
        {
            var logger = new ConsoleOnlyLogger();
            _emailSender = new InMemoryEmailSender(logger);
            _smsSender = new InMemorySmsSender(logger);
            _teamsSender = new InMemoryTeamsSender(logger);
            _dedupeStore = new InMemoryDeduplicationStore();
            var retryPolicy = new RetryPolicy(logger, maxRetries: 2, initialBackoffMs: 1);

            _sut = new NotificationDispatcher(
                _emailSender, _smsSender, _teamsSender,
                _dedupeStore, retryPolicy, logger);
        }

        [TestMethod]
        public void Dispatch_Email_SendsViaEmailProvider()
        {
            var msg = CreateMessage(NotificationType.Email);

            bool result = _sut.Dispatch(msg);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _emailSender.GetSentMessages().Count);
            Assert.AreEqual(0, _smsSender.GetSentMessages().Count);
            Assert.AreEqual(0, _teamsSender.GetSentMessages().Count);
        }

        [TestMethod]
        public void Dispatch_Sms_SendsViaSmsProvider()
        {
            var msg = CreateMessage(NotificationType.Sms);

            bool result = _sut.Dispatch(msg);

            Assert.IsTrue(result);
            Assert.AreEqual(0, _emailSender.GetSentMessages().Count);
            Assert.AreEqual(1, _smsSender.GetSentMessages().Count);
        }

        [TestMethod]
        public void Dispatch_Teams_SendsViaTeamsProvider()
        {
            var msg = CreateMessage(NotificationType.Teams);

            bool result = _sut.Dispatch(msg);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _teamsSender.GetSentMessages().Count);
        }

        [TestMethod]
        public void Dispatch_DuplicateMessage_IsSkipped()
        {
            var msg = CreateMessage(NotificationType.Sms);

            _sut.Dispatch(msg);
            _sut.Dispatch(msg); // duplicate

            Assert.AreEqual(1, _smsSender.GetSentMessages().Count);
        }

        private static NotificationMessage CreateMessage(NotificationType type)
        {
            return new NotificationMessage
            {
                MessageId = Guid.NewGuid(),
                Type = type,
                To = "test@contoso.com",
                Subject = "Test",
                Body = "Body",
                CorrelationId = Guid.NewGuid().ToString("N")
            };
        }
    }
}
