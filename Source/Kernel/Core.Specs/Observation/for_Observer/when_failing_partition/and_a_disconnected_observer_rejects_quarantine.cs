// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_disconnected_observer_rejects_quarantine : given.an_observer_automatically_reconciled_during_a_probe
{
    async Task Establish() => await _observer.Unsubscribe();

    async Task Because() => await _observer.PartitionFailed(_partition, 42UL, ["Late replay failure"], string.Empty, FailureKind.Disconnected);

    [Fact] async Task should_not_set_quarantine_for_a_rejected_transition() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_stay_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_keep_the_persisted_disconnected_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] async Task should_still_record_the_failure() => (await _observer.HasFailedPartitions()).ShouldBeTrue();
    [Fact] void should_not_subscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
