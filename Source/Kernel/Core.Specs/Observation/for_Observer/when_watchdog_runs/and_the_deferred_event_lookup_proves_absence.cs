// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_deferred_event_lookup_proves_absence : given.an_observer_waiting_for_event_lookup_retry
{
    void Establish() => _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
        .Returns(GetSequenceNumberError.NotFound);

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_stop_retrying_after_the_lookup_succeeds() => LookupCount().ShouldEqual(1);
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_be_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_fast_forward_past_the_tail() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_tailSequenceNumber.Next());
    [Fact] void should_not_start_catch_up() => ShouldNotHaveStartedCatchup();
}
