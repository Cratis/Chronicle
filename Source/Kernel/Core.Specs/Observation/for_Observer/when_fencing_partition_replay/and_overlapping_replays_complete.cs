// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_overlapping_replays_complete : given.an_observer_with_partition_replay_jobs
{
    bool _fencedAfterFirst;
    EventSequenceNumber _positionAfterFirst;
    int _writesAfterFirst;
    bool _firstCompletionRecorded;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _jobs = ImmutableList.Create(ReplayJob(_firstJob, _partition), ReplayJob(_secondJob, _partition));
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 42UL, [], _firstJob);
        _fencedAfterFirst = await _observer.IsPartitionFenced(_partition, _secondJob);
        _positionAfterFirst = _stateStorage.State.LastHandledEventSequenceNumber;
        _writesAfterFirst = _storageStats.Writes;
        _firstCompletionRecorded = _stateStorage.State.PartitionReplayCompletions!.Any(record => record.JobId == _firstJob && !record.HandedOff);

        // Both jobs still look running: Orleans persists completion only after this callback returns.
        await _observer.PartitionReplayed(_partition, 42UL, 42UL, [], _secondJob);
    }

    [Fact] void should_retain_the_fence_after_the_first_completion() => _fencedAfterFirst.ShouldBeTrue();
    [Fact] void should_not_advance_the_position_after_the_first_completion() => _positionAfterFirst.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_persist_the_first_completion_while_retaining_the_fence() => _writesAfterFirst.ShouldEqual(1);
    [Fact] void should_record_the_first_completion_without_a_handoff() => _firstCompletionRecorded.ShouldBeTrue();
    [Fact] async Task should_lift_the_fence_after_the_last_completion() => (await _observer.IsPartitionFenced(_partition, _secondJob)).ShouldBeFalse();
    [Fact] void should_persist_the_lifted_fence() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
