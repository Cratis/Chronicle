// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_committed_handoff_survives_reactivation : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            PartitionReplayCompletions = [new(_partition, _firstJob) { LastHandledEventSequenceNumber = 42UL, LastScannedEventSequenceNumber = 48UL, HandingOff = true }]
        };
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        EventSequenceHasNextEvent(48UL);
        SetReplayJobs(ReplayJob(_firstJob));
    }

    Task Because() => Crash();

    [Fact] void should_resume_the_owed_handoff() => CheckStartedCatchupJob(48UL, _partition);
    [Fact] void should_not_restore_a_completed_replay_fence() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
