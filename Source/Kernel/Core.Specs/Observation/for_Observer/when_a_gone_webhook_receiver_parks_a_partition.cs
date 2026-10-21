// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_receiver_parks_a_partition : given.a_webhook_observer
{
    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["410 Gone"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.ParkPartition);

    [Fact] void should_park_immediately() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_record_only_one_attempt() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
    [Fact] void should_not_pause_other_partitions() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_schedule_a_retry() => _failedPartitionsState.Partitions.Single().Deferral.ShouldBeNull();
}
