// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_the_empty_sequence_cannot_write_its_first_snapshot : given.an_empty_sequence_with_a_failing_first_snapshot
{
    AppendResult _first;
    AppendResult _second;
    int _writesAfterFirstAppend;
    bool _snapshotPersistedAfterFirstAppend;

    async Task Because()
    {
        _first = await AppendAnEvent();
        _writesAfterFirstAppend = _stateWriteAttempts;
        _snapshotPersistedAfterFirstAppend = _snapshots.Written is not null;
        _second = await AppendAnEvent();
    }

    [Fact] void should_keep_the_first_durable_append_successful() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_attempt_the_first_snapshot_after_appending() => _writesAfterFirstAppend.ShouldEqual(1);
    [Fact] void should_not_require_a_persisted_snapshot_for_the_first_append() => _snapshotPersistedAfterFirstAppend.ShouldBeFalse();
    [Fact] void should_keep_the_next_append_successful() => _second.IsSuccess.ShouldBeTrue();
    [Fact] void should_retry_the_snapshot_on_the_next_append() => _stateWriteAttempts.ShouldEqual(2);
    [Fact] void should_snapshot_the_advanced_watermark() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First + 2);
    [Fact] void should_dispatch_both_durable_events() => _appendedEventsQueues.Received(2).Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_update_both_events_constraint_indexes() => _constraintIndexSequenceNumbers.Count.ShouldEqual(2);
}
