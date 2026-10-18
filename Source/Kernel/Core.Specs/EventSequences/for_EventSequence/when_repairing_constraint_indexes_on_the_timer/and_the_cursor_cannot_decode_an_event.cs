// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_the_cursor_cannot_decode_an_event : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    protected override EventSequenceNumber InitialNext => 2;

    async Task Establish()
    {
        await Seed(0);
        await Seed(1, source: "poisoned");
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        var cursor = Substitute.For<IEventCursor>();
        var pages = 0;
        cursor.Current.Returns(_ => _log.Events.Take(1));
        cursor.MoveNext().Returns(_ => ++pages == 1 ? Task.FromResult(true) : Task.FromException<bool>(new FormatException("bad document")));
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(cursor);
        _eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>())
            .Returns(call => _log.GetNextSequenceNumberGreaterOrEqualThan(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<IEnumerable<EventType>?>(1), call.ArgAt<EventSourceId?>(2)));
        _eventSequenceStorage.GetEventAt(0).Returns(Task.FromResult(_log.Events[0]));
        _eventSequenceStorage.GetEventAt(1).Returns(Task.FromException<AppendedEvent>(new FormatException("bad document")));
    }

    async Task Because()
    {
        for (var attempt = 0; attempt < 7; attempt++)
        {
            await given.ConstraintRepairTimer.Tick(_silo);
            _clock.Advance(TimeSpan.FromSeconds(60));
        }
    }

    [Fact] async Task should_preserve_the_effect_of_the_preceding_event() => (await _claims.GetFor(_eventSourceId, _uniqueDefinition)).ShouldNotBeEmpty();
    [Fact] async Task should_advance_only_after_identifying_and_settling_the_bad_slot() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)2);
}
