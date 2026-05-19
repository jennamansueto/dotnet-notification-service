using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Application.Retry
{
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _initialBackoffMs;
        private readonly ILogger<RetryPolicy> _logger;

        public RetryPolicy(ILogger<RetryPolicy> logger, int maxRetries, int initialBackoffMs)
        {
            _logger = logger;
            _maxRetries = maxRetries;
            _initialBackoffMs = initialBackoffMs;
        }

        public async Task<bool> ExecuteAsync(Func<CancellationToken, Task> action, string operationName, string correlationId, CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    await action(cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (attempt < _maxRetries)
                    {
                        int delayMs = _initialBackoffMs * (int)Math.Pow(2, attempt - 1);
                        _logger.LogWarning(
                            "Attempt {Attempt}/{MaxRetries} failed for {OperationName} (CorrelationId={CorrelationId}): {Message}. Retrying in {DelayMs}ms.",
                            attempt, _maxRetries, operationName, correlationId, ex.Message, delayMs);
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogError(
                            "All {MaxRetries} attempts exhausted for {OperationName} (CorrelationId={CorrelationId}): {Message}",
                            _maxRetries, operationName, correlationId, ex.Message);
                        return false;
                    }
                }
            }

            return false;
        }
    }
}
