// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// Each kind of job keeps the holds it owns, with no cross-effect (#5406).
/// </summary>
public class and_a_partition_replay_job_and_an_observer_wide_catch_up_job_are_outstanding : given.a_routing_state_with_partition_holds
{
    void Establish()
    {
        HasOutstandingJobs(ObserverCatchUpJob(), PartitionReplayJob(_replayingPartition));
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_keep_the_catching_up_partition_held() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_catchingUpPartition);
    [Fact] void should_keep_the_replaying_partition_held() => _resultingStoredState.ReplayingPartitions.ShouldContain(_replayingPartition);
}
