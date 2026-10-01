// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_after_catchup_completed : given.a_quarantined_observer_with_remaining_events
{
    async Task Establish()
    {
        await _observer.CaughtUp(42UL);
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.ClearObserverQuarantine();

    [Fact] void should_catch_up_from_completed_position() => ShouldCatchUpFromRecordedPosition();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_be_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
