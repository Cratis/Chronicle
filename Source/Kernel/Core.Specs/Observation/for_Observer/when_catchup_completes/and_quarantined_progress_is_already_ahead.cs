// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_catchup_completes;

public class and_quarantined_progress_is_already_ahead : given.a_quarantined_observer
{
    void Establish() => _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 50UL, NextEventSequenceNumber = 55UL };

    async Task Because() => await _observer.CaughtUp(42UL);

    [Fact] void should_preserve_last_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)50UL);
    [Fact] void should_preserve_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)55UL);
    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
