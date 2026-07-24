// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_a_single_partition_is_in_flight : given.a_catching_up_in_flight_state
{
    readonly Key _partition = "shared-partition";

    void Establish() =>
        _storedState = _storedState with
        {
            LastHandledEventSequenceNumber = (EventSequenceNumber)4UL,
            InFlightPartitions = new HashSet<Key> { _partition }
        };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_exactly_one_catch_up_job_for_the_partition() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition));
}
