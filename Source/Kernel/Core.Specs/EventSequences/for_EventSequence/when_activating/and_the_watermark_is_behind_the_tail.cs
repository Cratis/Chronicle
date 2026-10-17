// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

public class and_the_watermark_is_behind_the_tail : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber InitialNext => 2;
    protected override EventSequenceNumber? Watermark => 1;
    EventSequenceNumber _tail;

    async Task Establish()
    {
        await Seed(0);
        await Seed(1);
    }
    async Task Because()
    {
        _tail = await _eventSequence.GetTailSequenceNumber();
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] void should_serve_reads_before_repair() => _tail.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_repair_before_validating() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_fold_only_the_lagging_range() => _claimWrites.ShouldEqual(1);
    [Fact] void should_start_reading_at_the_watermark() => _eventSequenceStorage.Received(1).GetFromSequenceNumber((EventSequenceNumber)1, eventTypes: Arg.Any<IEnumerable<EventType>>(), cancellationToken: Arg.Any<CancellationToken>());
}
