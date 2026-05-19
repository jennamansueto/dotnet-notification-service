using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Models;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;

namespace Contoso.NotificationRelay.UnitTests
{
    public class NotificationDispatcherTests
    {
        private readonly InMemoryEmailSender _emailSender;
        private readonly InMemorySmsSender _smsSender;
        private readonly InMemoryTeamsSender _teamsSender;
        private readonly InMemoryDeduplicationStore _dedupeStore;
        private readonly NotificationDispatcher _sut;

        public NotificationDispatcherTests()
        {
            _emailSender = new InMemoryEmailSender(NullLogger<InMemoryEmailSender>.Instance);
            _smsSender = new InMemorySmsSender(NullLogger<InMemorySmsSender>.Instance);
            _teamsSender = new InMemoryTeamsSender(NullLogger<InMemoryTeamsSender>.Instance);
            _dedupeStore = new InMemoryDeduplicationStore();
            var retryPolicy = new RetryPolicy(NullLogger<RetryPolicy>.Instance, maxRetries: 2, initialBackoffMs: 1);

            _sut = new NotificationDispatcher(
                _emailSender, _smsSender, _teamsSender,
                _dedupeStore, retryPolicy,
                NullLogger<NotificationDispatcher>.Instance);
        }

        [Fact]
        public async Task DispatchAsync_Email_SendsViaEmailProvider()
        {
            var msg = CreateMessage(NotificationType.Email);

            bool result = await _sut.DispatchAsync(msg);

            Assert.True(result);
            Assert.Single(_emailSender.GetSentMessages());
            Assert.Empty(_smsSender.GetSentMessages());
            Assert.Empty(_teamsSender.GetSentMessages());
        }

        [Fact]
        public async Task DispatchAsync_Sms_SendsViaSmsProvider()
        {
            var msg = CreateMessage(NotificationType.Sms);

            bool result = await _sut.DispatchAsync(msg);

            Assert.True(result);
            Assert.Empty(_emailSender.GetSentMessages());
            Assert.Single(_smsSender.GetSentMessages());
        }

        [Fact]
        public async Task DispatchAsync_Teams_SendsViaTeamsProvider()
        {
            var msg = CreateMessage(NotificationType.Teams);

            bool result = await _sut.DispatchAsync(msg);

            Assert.True(result);
            Assert.Single(_teamsSender.GetSentMessages());
        }

        [Fact]
        public async Task DispatchAsync_DuplicateMessage_IsSkipped()
        {
            var msg = CreateMessage(NotificationType.Sms);

            await _sut.DispatchAsync(msg);
            await _sut.DispatchAsync(msg); // duplicate

            Assert.Single(_smsSender.GetSentMessages());
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
