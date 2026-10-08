// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_the_observer_is_quarantined : given.a_quarantined_observer
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true, LastHandledEventSequenceNumber = 100UL, NextEventSequenceNumber = 101UL };
        _stateStorage.State.CatchingUpPartitions.Add("catching-up");
        _stateStorage.State.ReplayingPartitions.Add("replaying");
    }

    async Task Because()
    {
        await _observer.Replayed(42UL);
        await RunWatchdogTicks();
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_record_replay_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_record_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_finish_replaying() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] void should_clear_replaying_partitions() => _stateStorage.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_clear_catching_up_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_persist_completion() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_restart_replay() => ShouldNotHaveStartedReplay();
    [Fact] void should_not_resubscribe() => ShouldNotHaveResubscribed();
}
