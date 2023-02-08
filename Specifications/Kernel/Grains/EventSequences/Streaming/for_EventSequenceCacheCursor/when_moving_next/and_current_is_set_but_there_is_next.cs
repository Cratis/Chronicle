// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_current_is_set_but_there_is_next : given.a_cursor_and_an_empty_cache
{
    bool result;
    LinkedListNode<AppendedEvent> first_event;
    LinkedListNode<AppendedEvent> second_event;
    EventSequenceBatchContainer container;

    void Establish()
    {
        cache.SetupSequence(_ => _.HasEvent(EventSequenceNumber.First))
            .Returns(false)
            .Returns(true);

        var list = new LinkedList<AppendedEvent>();
        first_event = new LinkedListNode<AppendedEvent>(AppendedEvent.EmptyWithEventSequenceNumber(EventSequenceNumber.First));
        second_event = new LinkedListNode<AppendedEvent>(AppendedEvent.EmptyWithEventSequenceNumber(EventSequenceNumber.First + 1));
        list.AddLast(first_event);
        list.AddLast(second_event);

        cache.Setup(_ => _.GetEvent(EventSequenceNumber.First)).Returns(first_event);
        result = cursor.MoveNext();
    }

    void Because()
    {
        result = cursor.MoveNext();
        container = (cursor.GetCurrent(out var _) as EventSequenceBatchContainer)!;
    }

    [Fact] void should_not_prime_cache_for_second_event() => cache.Verify(_ => _.Prime(EventSequenceNumber.First), Once);
    [Fact] void should_return_true() => result.ShouldBeTrue();
    [Fact] void should_hold_the_next_event() => container.GetEvents<AppendedEvent>().First().Item1.ShouldEqual(second_event.Value);
}
