// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_the_partition_is_already_replaying : given.a_catching_up_in_flight_state
{
    readonly Key _partition = "partition";

    void Establish() => _storedState = _storedState with
    {
        InFlightPartitions = new HashSet<Key> { _partition },
        ReplayingPartitions = new HashSet<Key> { _partition }
    };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_start_another_job() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    [Fact] void should_remove_the_marker() => _resultingStoredState.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_not_mutate_the_handed_markers() => _storedState.InFlightPartitions.ShouldContain(_partition);
}
