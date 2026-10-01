// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_a_quarantined_observer_has_stranded_catchup_preparation : for_Observer.given.a_quarantined_observer
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 43UL };
        await _observer.CatchUp();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await RunWatchdogTicks();

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_leave_preparation_alone() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
    [Fact] void should_keep_the_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_start_catchup() => ShouldNotHaveStartedCatchup();
    [Fact] void should_not_resubscribe() => ShouldNotHaveResubscribed();
}
