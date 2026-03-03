using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contoso.NotificationRelay.Application.Retry;
using Contoso.NotificationRelay.Infrastructure.Logging;

namespace Contoso.NotificationRelay.UnitTests
{
    [TestClass]
    public class RetryPolicyTests
    {
        private readonly RetryPolicy _sut;

        public RetryPolicyTests()
        {
            _sut = new RetryPolicy(new ConsoleOnlyLogger(), maxRetries: 3, initialBackoffMs: 10);
        }

        [TestMethod]
        public void Execute_SucceedsOnFirstAttempt_ReturnsTrue()
        {
            int callCount = 0;

            bool result = _sut.Execute(() => { callCount++; }, "TestOp", "corr-1");

            Assert.IsTrue(result);
            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void Execute_FailsThenSucceeds_RetriesAndReturnsTrue()
        {
            int callCount = 0;

            bool result = _sut.Execute(() =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("transient");
            }, "TestOp", "corr-2");

            Assert.IsTrue(result);
            Assert.AreEqual(3, callCount);
        }

        [TestMethod]
        public void Execute_AllAttemptsFail_ReturnsFalse()
        {
            int callCount = 0;

            bool result = _sut.Execute(() =>
            {
                callCount++;
                throw new InvalidOperationException("permanent");
            }, "TestOp", "corr-3");

            Assert.IsFalse(result);
            Assert.AreEqual(3, callCount);
        }

        [TestMethod]
        public void Execute_AppliesBackoff_SecondRetrySlowerThanFirst()
        {
            var timestamps = new System.Collections.Generic.List<DateTimeOffset>();

            bool result = _sut.Execute(() =>
            {
                timestamps.Add(DateTimeOffset.UtcNow);
                if (timestamps.Count < 3) throw new InvalidOperationException("transient");
            }, "TestOp", "corr-4");

            Assert.IsTrue(result);
            if (timestamps.Count == 3)
            {
                TimeSpan gap1 = timestamps[1] - timestamps[0];
                TimeSpan gap2 = timestamps[2] - timestamps[1];
                Assert.IsTrue(gap2 >= gap1,
                    string.Format("Expected exponential backoff: gap2 ({0}ms) >= gap1 ({1}ms)",
                        gap2.TotalMilliseconds, gap1.TotalMilliseconds));
            }
        }
    }
}
