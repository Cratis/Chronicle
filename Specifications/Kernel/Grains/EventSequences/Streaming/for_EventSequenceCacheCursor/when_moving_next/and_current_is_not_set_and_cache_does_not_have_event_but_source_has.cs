// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_current_is_not_set_and_cache_does_not_have_event_but_source_has : given.a_cursor_and_an_empty_cache
{
    bool result;
    EventSequenceBatchContainer container;
    AppendedEvent @event;

    void Establish()
    {
        cache.SetupSequence(_ => _.HasEvent(EventSequenceNumber.First))
            .Returns(false)
            .Returns(true);

        @event = AppendedEvent.EmptyWithEventSequenceNumber(EventSequenceNumber.First);

        cache.Setup(_ => _.GetEvent(EventSequenceNumber.First)).Returns(new CachedAppendedEvent(@event));
    }

    void Because()
    {
        result = cursor.MoveNext();
        container = (cursor.GetCurrent(out var _) as EventSequenceBatchContainer)!;
    }

    [Fact] void should_prime_cache() => cache.Verify(_ => _.Prime(EventSequenceNumber.First), Once);
    [Fact] void should_get_event_from_cache() => cache.Verify(_ => _.GetEvent(EventSequenceNumber.First), Once);
    [Fact] void should_return_true() => result.ShouldBeTrue();
    [Fact] void should_hold_the_fetched_event() => container.GetEvents<AppendedEvent>().First().Item1.ShouldEqual(@event);
}
