// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_there_are_pending_entries : given.a_catching_up_in_flight_state
{
    Key _firstPartition = "partition-one";
    Key _secondPartition = "partition-two";

    void Establish()
    {
        _storedState = _storedState with { LastHandledEventSequenceNumber = (EventSequenceNumber)9UL };
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>())
            .Returns(new[]
            {
                new InFlightEvent { ObserverId = _observerKey.ObserverId, Partition = _firstPartition, EventSequenceNumber = (EventSequenceNumber)10UL },
                new InFlightEvent { ObserverId = _observerKey.ObserverId, Partition = _secondPartition, EventSequenceNumber = (EventSequenceNumber)11UL }
            });
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_catch_up_for_first_partition() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ =>
                _.Key == _firstPartition &&
                _.FromSequenceNumber == (EventSequenceNumber)10UL));

    [Fact] void should_start_catch_up_for_second_partition() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ =>
                _.Key == _secondPartition &&
                _.FromSequenceNumber == (EventSequenceNumber)10UL));

    [Fact] void should_mark_first_partition_as_catching_up() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_firstPartition);
    [Fact] void should_mark_second_partition_as_catching_up() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_secondPartition);
    [Fact] void should_transition_to_routing() => _observer.Received(1).TransitionTo<Routing>();
}
