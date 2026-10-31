// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// A partition's own replay job reports back through the observer, so the partition stays held while the job runs (#5379).
/// </summary>
public class and_a_partition_replay_job_runs_for_a_replaying_partition : given.a_routing_state_with_partition_holds
{
    void Establish() => HasOutstandingJobs(PartitionReplayJob(_replayingPartition));

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_keep_the_replaying_partition_held() => _resultingStoredState.ReplayingPartitions.ShouldContain(_replayingPartition);
    [Fact] void should_release_the_catching_up_partition_without_a_job() => _resultingStoredState.CatchingUpPartitions.ShouldBeEmpty();
}
