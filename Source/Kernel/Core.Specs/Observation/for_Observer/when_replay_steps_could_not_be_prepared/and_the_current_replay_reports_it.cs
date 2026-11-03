// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared;

public class and_the_current_replay_reports_it : given.a_replaying_observer
{
    ClaimEpoch _currentEpoch;

    void Establish() => _currentEpoch = _stateStorage.State.ObserverWideClaim!.Epoch;

    async Task Because()
    {
        await _observer.ReplayStepsCouldNotBePrepared(_currentEpoch.Value);
        await RunWatchdogTicks();
    }

    [Fact] async Task should_enter_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] void should_store_the_quarantined_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_replay_pending() => _stateStorage.State.IsReplaying.ShouldBeTrue();
}
