// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared;

public class and_the_report_carries_no_epoch : given.a_replaying_observer
{
    async Task Because()
    {
        await _observer.ReplayStepsCouldNotBePrepared(null);
        await RunWatchdogTicks();
    }

    [Fact] void should_hold_a_claim_the_report_does_not_name() => _stateStorage.State.ObserverWideClaim.ShouldNotBeNull();
    [Fact] async Task should_enter_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] void should_store_the_quarantined_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_replay_pending() => _stateStorage.State.IsReplaying.ShouldBeTrue();
}
