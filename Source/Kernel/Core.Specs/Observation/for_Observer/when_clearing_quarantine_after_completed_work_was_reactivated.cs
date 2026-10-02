// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_after_completed_work_was_reactivated : given.a_quarantined_observer_with_remaining_events
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _observer.Replayed(42UL);
        await Reactivate();
        (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
        _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
    }

    [Fact] void should_catch_up_from_persisted_position() => ShouldCatchUpFromRecordedPosition();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_be_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_not_restart_replay() => ShouldNotHaveStartedReplay();
}
