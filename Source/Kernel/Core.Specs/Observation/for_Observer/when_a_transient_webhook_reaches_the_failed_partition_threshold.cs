// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_transient_webhook_reaches_the_failed_partition_threshold : given.a_webhook_observer
{
    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, QuarantineOnFailedPartitionCount = 1 };

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling);

    [Fact] void should_keep_the_observer_running() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_schedule_the_next_attempt() => _failedPartitionsState.Partitions.Single().Deferral!.Reason.ShouldEqual(PartitionDeferralReason.RetryBackoff);
}
