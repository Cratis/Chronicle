// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// Ownership is per kind: a catch-up job does not report the partition's replay, so it does not keep the replay hold.
/// </summary>
public class and_a_held_partition_has_only_a_job_of_the_other_kind : given.a_routing_state_with_partition_holds
{
    void Establish() => HasOutstandingJobs(PartitionCatchUpJob(_replayingPartition));

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_release_the_replaying_partition() => _resultingStoredState.ReplayingPartitions.ShouldBeEmpty();
}
