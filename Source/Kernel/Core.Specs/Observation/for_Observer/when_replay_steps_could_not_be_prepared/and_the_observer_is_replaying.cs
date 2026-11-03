// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared;

public class and_the_observer_is_replaying : given.a_replaying_observer
{
    async Task Because()
    {
        await _observer.ReplayStepsCouldNotBePrepared();
        await RunWatchdogTicks();
    }

    [Fact] async Task should_enter_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] void should_store_the_quarantined_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_replay_pending() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_preserve_the_last_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43);
    [Fact] void should_not_start_another_replay() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_persist_the_state() => (_storageStats.Writes > 0).ShouldBeTrue();
}
