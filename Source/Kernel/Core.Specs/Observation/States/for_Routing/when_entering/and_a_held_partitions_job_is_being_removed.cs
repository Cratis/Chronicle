// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// A job being removed is still draining and still reports back, so its partition stays held (#5378, #5379).
/// </summary>
public class and_a_held_partitions_job_is_being_removed : given.a_routing_state_with_partition_holds
{
    void Establish() => HasOutstandingJobs(PartitionCatchUpJob(_catchingUpPartition, JobStatus.Removing));

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_keep_the_partition_held() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_catchingUpPartition);
}
