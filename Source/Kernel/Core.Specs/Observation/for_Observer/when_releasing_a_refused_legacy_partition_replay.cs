// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_legacy_partition_replay : when_partition_replayed.given.an_observer_with_replaying_partition
{
    PartitionRecoveryOutcome _recovery;

    void Establish() => _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 50UL, NextEventSequenceNumber = 51UL };

    async Task Because()
    {
        await _observer.ReleaseRefusedPartitionReplay(_partition, EventSequenceNumber.Unavailable);
        await _observer.ReleaseRefusedPartitionReplay(_partition, EventSequenceNumber.Unavailable);
        _recovery = await _observer.TryStartRecoverJobForFailedPartition(_partition);
    }

    [Fact] void should_release_the_marker() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_preserve_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)50UL);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)51UL);
    [Fact] void should_record_one_visible_failure() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_quarantine_the_partition() => _failedPartitionsStorage.State.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_record_an_unknown_position_not_a_guessed_cursor() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_explain_operator_recovery() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.Messages.Single().ShouldContain("no persisted held-back position");
    [Fact] void should_not_duplicate_the_failure_attempt() => _failedPartitionsStorage.State.Partitions.Single().Attempts.Count().ShouldEqual(1);
    [Fact] void should_not_automatically_recover_onto_the_unchanged_sink() => _recovery.ShouldEqual(PartitionRecoveryOutcome.PartitionQuarantined);
    [Fact] void should_not_start_catchup() => CheckDidNotStartCatchupJob();
    [Fact] void should_not_resolve_failures() => _failedPartitionsStorage.State.ResolvedPartitions.ShouldBeEmpty();
}
