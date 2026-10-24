// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_activation_restores_a_missing_replay_fence : given.an_observer_with_partition_replay_jobs
{
    void Establish() => SetReplayJobs(ReplayJob(_firstJob));

    Task Because() => Crash();

    [Fact] void should_restore_the_running_jobs_fence() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_not_start_catchup() => CheckDidNotStartCatchupJob();
}
