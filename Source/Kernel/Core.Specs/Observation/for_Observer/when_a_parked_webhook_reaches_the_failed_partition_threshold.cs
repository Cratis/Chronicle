// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_parked_webhook_reaches_the_failed_partition_threshold : given.a_webhook_observer
{
    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, QuarantineOnFailedPartitionCount = 1 };

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["410"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.ParkPartition);

    [Fact] void should_pause_the_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_partition_parked() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
}
