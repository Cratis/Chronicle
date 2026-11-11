// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_attempt_count_equals_the_retry_limit : given.an_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 1 };

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["Failure"], "Stack");

    [Fact] void should_not_quarantine_the_partition_yet() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
    [Fact] void should_register_the_retry() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id), TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1)), Times.Once);
}
