using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Models;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contoso.NotificationRelay.UnitTests;

[TestClass]
public class NotificationDispatcherTests
{
    private InMemoryEmailSender _emailSender = null!;
    private InMemorySmsSender _smsSender = null!;
    private InMemoryTeamsSender _teamsSender = null!;
    private InMemoryDeduplicationStore _dedupeStore = null!;
    private NotificationDispatcher _sut = null!;

    [TestInitialize]
    public void SetUp()
    {
        _emailSender = new InMemoryEmailSender(NullLogger<InMemoryEmailSender>.Instance);
        _smsSender = new InMemorySmsSender(NullLogger<InMemorySmsSender>.Instance);
        _teamsSender = new InMemoryTeamsSender(NullLogger<InMemoryTeamsSender>.Instance);
        _dedupeStore = new InMemoryDeduplicationStore();
        var retryPolicy = new RetryPolicy(NullLogger<RetryPolicy>.Instance, maxRetries: 2, initialBackoffMs: 1);

        _sut = new NotificationDispatcher(
            _emailSender, _smsSender, _teamsSender,
            _dedupeStore, retryPolicy, NullLogger<NotificationDispatcher>.Instance);
    }

    [TestMethod]
    public async Task Dispatch_Email_SendsViaEmailProvider()
    {
        var msg = CreateMessage(NotificationType.Email);

        bool result = await _sut.DispatchAsync(msg);

        Assert.IsTrue(result);
        Assert.AreEqual(1, _emailSender.GetSentMessages().Count);
        Assert.AreEqual(0, _smsSender.GetSentMessages().Count);
        Assert.AreEqual(0, _teamsSender.GetSentMessages().Count);
    }

    [TestMethod]
    public async Task Dispatch_Sms_SendsViaSmsProvider()
    {
        var msg = CreateMessage(NotificationType.Sms);

        bool result = await _sut.DispatchAsync(msg);

        Assert.IsTrue(result);
        Assert.AreEqual(0, _emailSender.GetSentMessages().Count);
        Assert.AreEqual(1, _smsSender.GetSentMessages().Count);
    }

    [TestMethod]
    public async Task Dispatch_Teams_SendsViaTeamsProvider()
    {
        var msg = CreateMessage(NotificationType.Teams);

        bool result = await _sut.DispatchAsync(msg);

        Assert.IsTrue(result);
        Assert.AreEqual(1, _teamsSender.GetSentMessages().Count);
    }

    [TestMethod]
    public async Task Dispatch_DuplicateMessage_IsSkipped()
    {
        var msg = CreateMessage(NotificationType.Sms);

        await _sut.DispatchAsync(msg);
        await _sut.DispatchAsync(msg); // duplicate

        Assert.AreEqual(1, _smsSender.GetSentMessages().Count);
    }

    private static NotificationMessage CreateMessage(NotificationType type) =>
        new()
        {
            MessageId = Guid.NewGuid(),
            Type = type,
            To = "test@contoso.com",
            Subject = "Test",
            Body = "Body",
            CorrelationId = Guid.NewGuid().ToString("N")
        };
}
