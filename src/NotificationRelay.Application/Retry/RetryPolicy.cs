using System;
using System.Threading;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Application.Retry
{
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _initialBackoffMs;
        private readonly ILogger _logger;

        public RetryPolicy(ILogger logger, int maxRetries, int initialBackoffMs)
        {
            _logger = logger;
            _maxRetries = maxRetries;
            _initialBackoffMs = initialBackoffMs;
        }

        public bool Execute(Action action, string operationName, string correlationId)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    if (attempt < _maxRetries)
                    {
                        int delayMs = _initialBackoffMs * (int)Math.Pow(2, attempt - 1);
                        _logger.Warn(string.Format(
                            "Attempt {0}/{1} failed for {2} (CorrelationId={3}): {4}. Retrying in {5}ms.",
                            attempt, _maxRetries, operationName, correlationId, ex.Message, delayMs));
                        Thread.Sleep(delayMs);
                    }
                    else
                    {
                        _logger.Error(string.Format(
                            "All {0} attempts exhausted for {1} (CorrelationId={2}): {3}",
                            _maxRetries, operationName, correlationId, ex.Message));
                        return false;
                    }
                }
            }

            return false;
        }
    }
}
