// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_overlapping_completions_keep_the_furthest_read : given.an_observer_with_partition_replay_jobs
{
    bool _held;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        SetReplayJobs(ReplayJob(_firstJob), ReplayJob(_secondJob));
        EventSequenceHasNextEvent(90UL);
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 90UL, [], _firstJob);
        _held = _stateStorage.State.ReplayingPartitions.Contains(_partition);
        await _observer.PartitionReplayed(_partition, 30UL, 48UL, [], _secondJob);
        await _observer.PartitionReplayed(_partition, 30UL, 48UL, [], _secondJob);
    }

    [Fact] void should_wait_for_the_other_replay() => _held.ShouldBeTrue();
    [Fact] void should_hand_off_once_from_the_furthest_read() => CheckStartedCatchupJob(90UL, _partition);
    [Fact] async Task should_not_refence_the_successor_while_completed_jobs_are_running() => (await _observer.IsPartitionFenced(_partition, Cratis.Orleans.Jobs.JobId.New())).ShouldBeFalse();
}
