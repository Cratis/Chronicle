// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_a_late_unique_index_requires_the_deferred_snapshot : given.an_empty_sequence_with_a_failing_first_snapshot
{
    AppendResult _first;
    AppendResult _retry;
    EventSequenceNumber _committedAfterFailure;

    void Establish() => _constraintValidation.IndexedDefinitions.Returns(
        [new UniqueConstraintDefinition("unique", [new UniqueConstraintEventDefinition(_eventType.Id, ["Some"])])]);

    async Task Because()
    {
        _first = await AppendAnEvent();
        _committedAfterFailure = _appendedSequenceNumber;
        _retry = await AppendAnEvent();
    }

    [Fact] void should_refuse_the_append_without_a_durable_watermark() => _first.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_commit_before_the_snapshot_succeeds() => _committedAfterFailure.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_allow_a_retry_after_the_snapshot_succeeds() => _retry.IsSuccess.ShouldBeTrue();
    [Fact] void should_persist_the_initial_watermark_before_committing() => _snapshots.Snapshots[0].ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_persist_the_initial_sequence_before_committing() => _snapshots.Snapshots[0].SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_start_historical_catch_up() => _snapshots.Snapshots[0].ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_not_set_a_refusal_marker() => _snapshots.Snapshots[0].ConstraintIndexCatchUpRefusing.ShouldBeNull();
    [Fact] void should_retry_initialization_before_the_post_commit_snapshot() => _stateWriteAttempts.ShouldEqual(3);
    [Fact] void should_update_only_the_committed_events_indexes() => _constraintIndexSequenceNumbers.Count.ShouldEqual(1);
}
