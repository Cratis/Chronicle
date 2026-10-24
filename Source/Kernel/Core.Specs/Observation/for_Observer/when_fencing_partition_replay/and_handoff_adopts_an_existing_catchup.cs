// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_handoff_adopts_an_existing_catchup : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        // Prepare persisted the lifted fence and handoff before this successor started; activation was then lost.
        _stateStorage.State.ReplayingPartitions.Remove(_partition);
        _stateStorage.State = _stateStorage.State with { PartitionReplayCompletions = [new(_partition, _firstJob) { LastHandledEventSequenceNumber = 42UL, LastScannedEventSequenceNumber = 48UL, HandingOff = true }] };
        SetCatchupJobs(CatchupJob(_secondJob));
    }

    Task Because() => Crash();

    [Fact] void should_not_start_a_duplicate_catchup() => CheckDidNotStartCatchupJob();
    [Fact] void should_adopt_partition_ownership() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_record_the_handoff() => _stateStorage.State.PartitionReplayCompletions!.Single().HandedOff.ShouldBeTrue();
    [Fact] void should_keep_the_marker_lifted() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
