// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_half_the_non_webhook_partitions_have_failed : given.an_observer_with_subscription
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, QuarantineOnFailedPartitionPercentage = 0.5 };

    void Establish()
    {
        _failedPartitionsState.RegisterAttempt("resolved", 41UL, ["Earlier failure"], "Stack", FailureKind.Handling);
        _failedPartitionsState.Remove("resolved");
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["Failure"], "Stack");

    [Fact] void should_not_quarantine_at_the_exact_percentage_threshold() => (_stateStorage.State.RunningState == ObserverRunningState.Quarantined).ShouldBeFalse();
    [Fact] void should_leave_the_failed_partition_retryable() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
}
