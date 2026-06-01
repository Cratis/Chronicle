// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_observer_is_quarantined : given.a_catching_up_in_flight_state
{
    Key _partition = "some-partition";

    void Establish()
    {
        _storedState = _storedState with { RunningState = ObserverRunningState.Quarantined };
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>())
            .Returns(
            [
                new InFlightEvent { ObserverId = _observerKey.ObserverId, Partition = _partition, EventSequenceNumber = (EventSequenceNumber)1UL }
            ]);
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_start_any_catch_up_jobs() => _jobsManager
        .DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());

    [Fact] void should_transition_to_routing() => _observer.Received(1).TransitionTo<Routing>();
}
