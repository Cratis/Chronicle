// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_there_are_no_in_flight_entries : given.a_catching_up_in_flight_state
{
    void Establish() => _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>()).Returns([]);

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_start_any_catch_up_jobs() => _jobsManager
        .DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());

    [Fact] void should_transition_to_routing() => _observer.Received(1).TransitionTo<Routing>();
}
