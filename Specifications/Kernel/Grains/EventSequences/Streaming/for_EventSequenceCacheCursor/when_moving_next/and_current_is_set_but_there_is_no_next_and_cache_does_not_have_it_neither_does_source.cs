// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_current_is_set_but_there_is_no_next_and_cache_does_not_have_it_neither_does_source : given.a_cursor_and_an_empty_cache
{
    bool result;
    CachedAppendedEvent first_event;

    void Establish()
    {
        cache.SetupSequence(_ => _.HasEvent(EventSequenceNumber.First))
            .Returns(false)
            .Returns(true);

        cache.SetupSequence(_ => _.HasEvent(EventSequenceNumber.First + 1))
            .Returns(false)
            .Returns(false);

        first_event = new CachedAppendedEvent(AppendedEvent.EmptyWithEventSequenceNumber(EventSequenceNumber.First));
        cache.Setup(_ => _.GetEvent(EventSequenceNumber.First)).Returns(first_event);
        result = cursor.MoveNext();
    }

    void Because() => result = cursor.MoveNext();

    [Fact] void should_prime_cache_for_second_event() => cache.Verify(_ => _.Prime(EventSequenceNumber.First + 1), Once);
    [Fact] void should_return_false() => result.ShouldBeFalse();
}
