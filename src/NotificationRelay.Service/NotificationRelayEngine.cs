using System;
using System.Threading;
using Contoso.NotificationRelay.Application.Dispatching;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Domain.Interfaces;
using Contoso.NotificationRelay.Infrastructure.Deduplication;
using Contoso.NotificationRelay.Infrastructure.Providers;
using Contoso.NotificationRelay.Infrastructure.Queue;

namespace Contoso.NotificationRelay.Service
{
    /// <summary>
    /// Core processing engine. Shared between console mode and Windows Service mode.
    /// Wires up all dependencies (poor man's DI) and runs the poll loop.
    /// </summary>
    public class NotificationRelayEngine : IDisposable
    {
        private readonly ILogger _logger;
        private readonly InMemoryQueueConsumer _queueConsumer;
        private readonly NotificationDispatcher _dispatcher;
        private volatile bool _running;
        private Thread _workerThread;

        public NotificationRelayEngine(ILogger logger)
        {
            _logger = logger;

            // Poor man's DI — wire up infrastructure
            _queueConsumer = new InMemoryQueueConsumer(_logger);
            var emailSender = new InMemoryEmailSender(_logger);
            var smsSender = new InMemorySmsSender(_logger);
            var teamsSender = new InMemoryTeamsSender(_logger);
            var dedupeStore = new InMemoryDeduplicationStore();
            var retryPolicy = new RetryPolicy(_logger, AppSettings.RetryCount, AppSettings.RetryBackoffMs);

            _dispatcher = new NotificationDispatcher(
                emailSender, smsSender, teamsSender,
                dedupeStore, retryPolicy, _logger);
        }

        public void Start()
        {
            _running = true;
            _workerThread = new Thread(PollLoop)
            {
                Name = "NotificationRelayWorker",
                IsBackground = true
            };
            _workerThread.Start();
            _logger.Info("NotificationRelayEngine started.");
        }

        public void Stop()
        {
            _logger.Info("NotificationRelayEngine stopping...");
            _running = false;
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(TimeSpan.FromSeconds(10));
            }
            _logger.Info("NotificationRelayEngine stopped.");
        }

        private void PollLoop()
        {
            int pollIntervalMs = AppSettings.PollIntervalMs;

            while (_running)
            {
                try
                {
                    var message = _queueConsumer.Dequeue();

                    if (message == null)
                    {
                        Thread.Sleep(pollIntervalMs);
                        continue;
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
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("Unhandled exception in poll loop: {0}", ex));
                    Thread.Sleep(5000);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            var disposableLogger = _logger as IDisposable;
            if (disposableLogger != null)
            {
                disposableLogger.Dispose();
            }
        }
    }
}
