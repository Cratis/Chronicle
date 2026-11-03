// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_not_recovered;

public class and_the_backoff_factor_is_zero : given.an_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, BackoffDelay = 5, ExponentialBackoffDelayFactor = 0, MaximumBackoffDelay = 600 };

    void Establish() => _failedPartitionsState.RegisterAttempt("partition", 42UL, ["Failed"], "Stack");

    async Task Because() => await _observer.FailedPartitionNotRecovered("partition");

    [Fact] void should_fall_back_to_the_base_delay() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id), TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1)), Times.Once);
    [Fact] void should_keep_the_count() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_the_partition_failed() => _failedPartitionsState.Partitions.Single().IsResolved.ShouldBeFalse();
}
