// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

/// <summary>
/// With unlimited retries the exponential backoff grows beyond what a time span can hold. The delay is then the maximum backoff delay.
/// </summary>
public class and_unlimited_retries_overflow_the_backoff : given.an_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 0 };

    void Establish()
    {
        for (var index = 0; index < 1100; index++)
        {
            _failedPartitionsState.RegisterAttempt("partition", 42UL, ["failure"], "", FailureKind.Handling);
        }
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["failure"], "", FailureKind.Handling);

    [Fact] void should_register_the_retry_reminder_with_the_maximum_backoff_delay() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(600)), Times.Once);
    [Fact] void should_not_quarantine_the_partition() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
}
