// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_partition_is_already_failed : given.a_catching_up_in_flight_state
{
    Key _partition = "failing-partition";

    void Establish()
    {
        _failedPartitions.AddFailedPartition(_partition);
        _storedState = _storedState with { LastHandledEventSequenceNumber = (EventSequenceNumber)4UL };
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>())
            .Returns(new[]
            {
                new InFlightEvent { ObserverId = _observerKey.ObserverId, Partition = _partition, EventSequenceNumber = (EventSequenceNumber)5UL }
            });
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_start_any_catch_up_job_for_failing_partition() => _jobsManager
        .DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition));

    [Fact] void should_transition_to_routing() => _observer.Received(1).TransitionTo<Routing>();
}
