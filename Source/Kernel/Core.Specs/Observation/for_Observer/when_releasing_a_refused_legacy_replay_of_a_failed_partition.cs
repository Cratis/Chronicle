// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_legacy_replay_of_a_failed_partition : when_partition_replayed.given.an_observer_with_replaying_partition
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
    }

    async Task Because() => await _observer.ReleaseRefusedPartitionReplay(_partition, EventSequenceNumber.Unavailable);

    [Fact] void should_release_the_marker() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_preserve_failure_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_preserve_the_existing_failure_position() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.SequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_preserve_failure_history() => _failedPartitionsStorage.State.Partitions.Single().Attempts.Count().ShouldEqual(2);
    [Fact] void should_quarantine_existing_failure() => _failedPartitionsStorage.State.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_not_resolve_existing_failure() => _failedPartitionsStorage.State.ResolvedPartitions.ShouldBeEmpty();
    [Fact] void should_preserve_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
}
