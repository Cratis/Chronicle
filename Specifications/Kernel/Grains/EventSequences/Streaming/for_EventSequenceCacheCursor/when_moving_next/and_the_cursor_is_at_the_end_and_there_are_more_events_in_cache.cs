// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_the_cursor_is_at_the_end_and_there_are_more_events_in_cache : given.cursor_with_ten_events_from_cache
{
    bool result;
    AppendedEvent[] next_events;
    EventSequenceBatchContainer current;

    void Establish()
    {
        Enumerable.Range(0, 10).ForEach(_ => cursor.MoveNext());

        next_events = Enumerable.Range(0, 10).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToArray();
        var sortedEvents = new SortedSet<AppendedEvent>(next_events, new AppendedEventComparer());
        cache.Setup(_ => _.GetView(IsAny<EventSequenceNumber>(), IsAny<EventSequenceNumber>())).Returns(sortedEvents);
    }

    void Because()
    {
        result = cursor.MoveNext();
        current = (EventSequenceBatchContainer)cursor.GetCurrent(out var _);
    }

    [Fact] void should_move() => result.ShouldBeTrue();
    [Fact] void should_have_correct_current_event() => current.GetEvents<AppendedEvent>().Single().Item1.Metadata.SequenceNumber.ShouldEqual(next_events[0].Metadata.SequenceNumber);
}
