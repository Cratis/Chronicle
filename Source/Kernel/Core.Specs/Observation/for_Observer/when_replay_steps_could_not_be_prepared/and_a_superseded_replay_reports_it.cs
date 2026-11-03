// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared;

public class and_a_superseded_replay_reports_it : given.a_replaying_observer
{
    ClaimEpoch _currentEpoch;
    ulong _staleEpoch;

    void Establish()
    {
        _currentEpoch = _stateStorage.State.ObserverWideClaim!.Epoch;
        _staleEpoch = _currentEpoch.Value - 1;
    }

    Task Because() => _observer.ReplayStepsCouldNotBePrepared(_staleEpoch);

    [Fact] void should_hold_a_newer_claim_than_the_report() => (_currentEpoch.Value > _staleEpoch).ShouldBeTrue();
    [Fact] async Task should_stay_in_the_replay_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] async Task should_not_quarantine_the_observer() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_not_store_a_quarantined_running_state() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_replaying() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_keep_the_newer_claim() => _stateStorage.State.ObserverWideClaim!.Epoch.ShouldEqual(_currentEpoch);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
