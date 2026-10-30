// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_no_partitions_are_held : given.a_routing_state_with_partition_holds
{
    void Establish() => _storedState = _storedState with
    {
        CatchingUpPartitions = new HashSet<Concepts.Keys.Key>(),
        ReplayingPartitions = new HashSet<Concepts.Keys.Key>()
    };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_look_up_any_jobs() => _jobStorage.DidNotReceive().GetJobs(Arg.Any<JobQuery>());
}
