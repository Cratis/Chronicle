// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_no_events_were_handled_while_quarantined : given.a_quarantined_observer
{
    void Establish() => _stateStorage.State = _stateStorage.State with { IsReplaying = true };

    async Task Because() => await _observer.Replayed(EventSequenceNumber.Unavailable);

    [Fact] void should_start_from_the_first_event_after_clearing() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_record_no_handled_events() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_finish_replaying() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
