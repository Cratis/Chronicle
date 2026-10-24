// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_fenced_predecessor_has_not_finalized : given.an_observer_with_partition_replay_jobs
{
    bool _sawFence;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        SetCatchupJobs(CatchupJob(_secondJob));
        EventSequenceHasNextEvent(48UL);
    }

    async Task Because()
    {
        _sawFence = await _observer.IsPartitionFenced(_partition, _secondJob);
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
    }

    [Fact] void should_have_fenced_the_predecessor() => _sawFence.ShouldBeTrue();
    [Fact] void should_keep_the_marker() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_not_prepare_the_handoff() => _stateStorage.State.PartitionReplayCompletions!.Single().HandingOff.ShouldBeFalse();
    [Fact] void should_not_record_a_handoff() => _stateStorage.State.PartitionReplayCompletions!.Single().HandedOff.ShouldBeFalse();
    [Fact] void should_not_adopt_or_start_a_successor() => CheckDidNotStartCatchupJob();
    [Fact] void should_schedule_a_bounded_retry() => (RetryTimerIndex >= 0).ShouldBeTrue();
}
