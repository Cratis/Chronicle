// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_there_is_a_gap_in_the_sequence : given.all_dependencies
{
    EventSequenceQueueCacheCursor cursor;
    EventSequenceBatchContainer current;

    void Establish()
    {
        var events = Enumerable.Range(0, 4).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToList();
        events.AddRange(Enumerable.Range(6, 4).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)));
        var sortedEvents = new SortedSet<AppendedEvent>(events, new AppendedEventComparer());
        cache.Setup(_ => _.GetView(0, null)).Returns(sortedEvents);

        var secondEvents = Enumerable.Range(4, 5).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToList();
        var secondSortedEvents = new SortedSet<AppendedEvent>(secondEvents, new AppendedEventComparer());

        cache.SetupSequence(_ => _.GetView(IsAny<EventSequenceNumber>(), IsAny<EventSequenceNumber>()))
            .Returns(sortedEvents)
            .Returns(new SortedSet<AppendedEvent>(new AppendedEventComparer()))
            .Returns(secondSortedEvents);

        cursor = new EventSequenceQueueCacheCursor(
            cache.Object,
            microservice_id,
            tenant_id,
            event_sequence_id,
            EventSequenceNumber.First);

        Enumerable.Range(0, 4).ForEach(_ => cursor.MoveNext());
    }

    void Because()
    {
        cursor.MoveNext();
        current = (EventSequenceBatchContainer)cursor.GetCurrent(out var _);
    }

    [Fact] void should_prime_the_cache() => cache.Verify(_ => _.Prime(4L), Once);
    [Fact] void should_have_correct_current_event() => current.GetEvents<AppendedEvent>().Single().Item1.Metadata.SequenceNumber.ShouldEqual((EventSequenceNumber)4L);
}
