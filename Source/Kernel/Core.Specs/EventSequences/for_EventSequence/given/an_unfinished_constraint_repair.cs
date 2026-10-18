// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_unfinished_constraint_repair : an_event_sequence_with_repairable_indexes
{
    protected readonly RepairClock _clock = new();
    protected AppendResult _first;
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        var stop = true;
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _rangeReads++;
                if (stop)
                {
                    stop = false;
                    _clock.Advance(TimeSpan.FromMilliseconds(4750));
                }
                return new EventCursor(_log.Events.Where(e => e.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0) && call.ArgAt<IEnumerable<EventType>>(5).Any(t => t.Id == e.Context.EventType.Id)).ToList());
            });
        _first = await AppendAnEvent();
    }
}
