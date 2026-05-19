using Microsoft.Extensions.Logging;

namespace Contoso.NotificationRelay.Application.Retry;

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

    public async Task<bool> ExecuteAsync(Func<Task> action, string operationName, string correlationId, CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt < _maxRetries)
                {
                    int delayMs = _initialBackoffMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning(
                        "Attempt {Attempt}/{MaxRetries} failed for {Operation} (CorrelationId={CorrelationId}): {Error}. Retrying in {DelayMs}ms.",
                        attempt, _maxRetries, operationName, correlationId, ex.Message, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
                else
                {
                    _logger.LogError(
                        "All {MaxRetries} attempts exhausted for {Operation} (CorrelationId={CorrelationId}): {Error}",
                        _maxRetries, operationName, correlationId, ex.Message);
                    return false;
                }
            }
        }

        return false;
    }
}
