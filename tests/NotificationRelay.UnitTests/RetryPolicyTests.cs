using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Contoso.NotificationRelay.Application.Retry;

namespace Contoso.NotificationRelay.UnitTests
{
    public class RetryPolicyTests
    {
        private readonly RetryPolicy _sut;

        public RetryPolicyTests()
        {
            _sut = new RetryPolicy(NullLogger<RetryPolicy>.Instance, maxRetries: 3, initialBackoffMs: 50);
        }

        [Fact]
        public async Task ExecuteAsync_SucceedsOnFirstAttempt_ReturnsTrue()
        {
            int callCount = 0;

            bool result = await _sut.ExecuteAsync(ct => { callCount++; return Task.CompletedTask; }, "TestOp", "corr-1");

            Assert.True(result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteAsync_FailsThenSucceeds_RetriesAndReturnsTrue()
        {
            int callCount = 0;

            bool result = await _sut.ExecuteAsync(ct =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("transient");
                return Task.CompletedTask;
            }, "TestOp", "corr-2");

            Assert.True(result);
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task ExecuteAsync_AllAttemptsFail_ReturnsFalse()
        {
            int callCount = 0;

            bool result = await _sut.ExecuteAsync(ct =>
            {
                callCount++;
                throw new InvalidOperationException("permanent");
            }, "TestOp", "corr-3");

            Assert.False(result);
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task ExecuteAsync_AppliesBackoff_SecondRetrySlowerThanFirst()
        {
            var timestamps = new List<DateTimeOffset>();

            bool result = await _sut.ExecuteAsync(ct =>
            {
                timestamps.Add(DateTimeOffset.UtcNow);
                if (timestamps.Count < 3) throw new InvalidOperationException("transient");
                return Task.CompletedTask;
            }, "TestOp", "corr-4");

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
