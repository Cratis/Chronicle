// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_the_cursor_is_at_the_end_and_there_are_no_more_events_in_cache_but_sequence_has : given.cursor_with_ten_events_from_cache
{
    bool result;
    EventSequenceBatchContainer current;
    AppendedEvent event_in_sequence;

    void Establish()
    {
        Enumerable.Range(0, 10).ForEach(_ => cursor.MoveNext());
        event_in_sequence = AppendedEvent.EmptyWithEventSequenceNumber(10);
        cache.SetupSequence(_ => _.GetView(IsAny<EventSequenceNumber>(), IsAny<EventSequenceNumber>()))
            .Returns(new SortedSet<AppendedEvent>())
            .Returns(new SortedSet<AppendedEvent>(new[] { event_in_sequence }));
    }

    void Because()
    {
        result = cursor.MoveNext();
        current = (EventSequenceBatchContainer)cursor.GetCurrent(out var _);
    }

    [Fact] void should_move() => result.ShouldBeTrue();
    [Fact] void should_prime_cache() => cache.Verify(_ => _.Prime(10L), Once);
    [Fact] void should_have_correct_current_event() => current.GetEvents<AppendedEvent>().Single().Item1.Metadata.SequenceNumber.ShouldEqual(event_in_sequence.Metadata.SequenceNumber);
}
