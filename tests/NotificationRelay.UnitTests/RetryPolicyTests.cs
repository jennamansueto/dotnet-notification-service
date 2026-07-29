using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Contoso.NotificationRelay.Application.Retry;
using Microsoft.Extensions.Logging.Abstractions;

namespace Contoso.NotificationRelay.UnitTests
{
    public class RetryPolicyTests
    {
        private readonly RetryPolicy _sut;

        public RetryPolicyTests()
        {
            _sut = new RetryPolicy(NullLogger<RetryPolicy>.Instance, maxRetries: 3, initialBackoffMs: 10);
        }

        [Fact]
        public async Task Execute_SucceedsOnFirstAttempt_ReturnsTrue()
        {
            int callCount = 0;

            bool result = await _sut.ExecuteAsync(() =>
            {
                callCount++;
                return Task.CompletedTask;
            }, "TestOp", "corr-1", CancellationToken.None);

            Assert.True(result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task Execute_FailsThenSucceeds_RetriesAndReturnsTrue()
        {
            int callCount = 0;

            bool result = await _sut.ExecuteAsync(() =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("transient");
                return Task.CompletedTask;
            }, "TestOp", "corr-2", CancellationToken.None);

            Assert.True(result);
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task Execute_AllAttemptsFail_ReturnsFalse()
        {
            int callCount = 0;

            bool result = await _sut.ExecuteAsync(() =>
            {
                callCount++;
                throw new InvalidOperationException("permanent");
            }, "TestOp", "corr-3", CancellationToken.None);

            Assert.False(result);
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task Execute_AppliesBackoff_SecondRetrySlowerThanFirst()
        {
            var timestamps = new System.Collections.Generic.List<DateTimeOffset>();

            bool result = await _sut.ExecuteAsync(() =>
            {
                timestamps.Add(DateTimeOffset.UtcNow);
                if (timestamps.Count < 3) throw new InvalidOperationException("transient");
                return Task.CompletedTask;
            }, "TestOp", "corr-4", CancellationToken.None);

            Assert.True(result);
            if (timestamps.Count == 3)
            {
                TimeSpan gap1 = timestamps[1] - timestamps[0];
                TimeSpan gap2 = timestamps[2] - timestamps[1];
                Assert.True(gap2 >= gap1,
                    string.Format("Expected exponential backoff: gap2 ({0}ms) >= gap1 ({1}ms)",
                        gap2.TotalMilliseconds, gap1.TotalMilliseconds));
            }
        }
    }
}
