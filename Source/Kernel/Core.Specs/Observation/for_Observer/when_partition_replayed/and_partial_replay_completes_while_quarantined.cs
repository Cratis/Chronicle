// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_partial_replay_completes_while_quarantined : given.an_observer_with_replaying_partition
{
    async Task Establish()
    {
        _observer.SetSubscription(new(_observerId, _observerKey, [EventType.Unknown], typeof(ObserverSubscriber), SiloAddress.Zero, null));
        await _observer.TransitionTo<Routing>();
        await _observer.TransitionTo<QuarantinedObserver>();
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 45UL };
        EventSequenceHasNextEvent(42UL);
        _storageStats.ResetCounts();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.PartitionReplayPartiallyCompleted(_partition, 42UL);
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_clear_the_replaying_marker() => _stateStorage.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_preserve_the_higher_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)45UL);
    [Fact] void should_retain_required_partition_continuation() => CheckStartedCatchupJob(42UL);
    [Fact] void should_record_the_continuation_marker() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_persist_completion_and_continuation() => _storageStats.Writes.ShouldEqual(2);
}
