// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.and_recovery_after_quarantine_is_owed;

public class and_it_was_stranded_in_catching_up : for_Observer.given.an_observer_whose_quarantine_leave_fails_to_persist
{
    /// <summary>
    /// Gets the running state of CatchingUpInFlight; failing its write strands the observer there.
    /// </summary>
    protected override ObserverRunningState FailingEntry => ObserverRunningState.Unknown;

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_be_stranded_in_catching_up_before_the_watchdog() => _stateAfterFailure.ShouldEqual(typeof(CatchingUpInFlight));
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_active_state() => _faultableStateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_no_longer_owe_recovery() => _observer.OwesRecoveryAfterQuarantine().ShouldBeFalse();
}
