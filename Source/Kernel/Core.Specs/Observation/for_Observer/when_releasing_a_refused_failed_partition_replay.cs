// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_failed_partition_replay : when_partition_replayed.given.an_observer_with_replaying_partition
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
    }

    async Task Because() => await _observer.ReleaseRefusedPartitionReplay(_partition, 45UL);

    [Fact] void should_remove_the_replay_marker() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_preserve_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
    [Fact] void should_not_resolve_failures() => _failedPartitionsStorage.State.ResolvedPartitions.ShouldBeEmpty();
    [Fact] void should_keep_the_failure() => _failedPartitionsStorage.State.Partitions.Count().ShouldEqual(1);
    [Fact] void should_preserve_failure_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_leave_recovery_to_the_failure_retry() => CheckDidNotStartCatchupJob();
}
