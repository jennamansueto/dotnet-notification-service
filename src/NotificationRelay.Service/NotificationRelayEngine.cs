using System;
using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Infrastructure.Queue;

namespace Contoso.NotificationRelay.Service
{
    /// <summary>
    /// Core processing engine. Wires up all dependencies (poor man's DI) and
    /// exposes a single-iteration <see cref="PollOnce"/> method that the
    /// <see cref="NotificationRelayWorker"/> BackgroundService calls each cycle.
    /// </summary>
    public class NotificationRelayEngine : IDisposable
    {
        private readonly ILogger _logger;
        private readonly InMemoryQueueConsumer _queueConsumer;
        private readonly NotificationDispatcher _dispatcher;

        public NotificationRelayEngine(ILogger logger, NotificationRelayOptions options)
        {
            _logger = logger;

            // Poor man's DI — wire up infrastructure
            _queueConsumer = new InMemoryQueueConsumer(_logger);
            var emailSender = new InMemoryEmailSender(_logger);
            var smsSender = new InMemorySmsSender(_logger);
            var teamsSender = new InMemoryTeamsSender(_logger);
            var dedupeStore = new InMemoryDeduplicationStore();
            var retryPolicy = new RetryPolicy(_logger, options.RetryCount, options.RetryBackoffMs);

            _dispatcher = new NotificationDispatcher(
                emailSender, smsSender, teamsSender,
                dedupeStore, retryPolicy, _logger);
        }

        /// <summary>
        /// Performs a single poll iteration: dequeues one message and dispatches it.
        /// Called repeatedly by the BackgroundService loop.
        /// </summary>
        public void PollOnce()
        {
            var message = _queueConsumer.Dequeue();

            if (message == null)
            {
                return;
            }

            _logger.Info(string.Format(
                "Processing {0} message for {1} (MessageId={2}, CorrelationId={3}).",
                message.Type, message.To, message.MessageId, message.CorrelationId));

            bool success = _dispatcher.Dispatch(message);

            if (!success)
            {
                _logger.Error(string.Format(
                    "Failed to dispatch MessageId={0} after retries.",
                    message.MessageId));
            }
        }

        public void Dispose()
        {
            // The logger lifetime is now managed by the DI container / Program.cs,
            // so we no longer dispose it here.
        }
    }
}
