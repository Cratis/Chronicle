// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_the_cursor_is_at_the_end_and_there_are_no_more_events_in_cache_nor_in_sequence : given.cursor_with_ten_events_from_cache
{
    bool result;

    void Establish()
    {
        Enumerable.Range(0, 10).ForEach(_ => cursor.MoveNext());
        cache.Setup(_ => _.GetView(IsAny<EventSequenceNumber>(), IsAny<EventSequenceNumber>())).Returns(new SortedSet<AppendedEvent>());
    }

    void Because() => result = cursor.MoveNext();

    [Fact] void should_move() => result.ShouldBeFalse();
    [Fact] void should_prime_cache() => cache.Verify(_ => _.Prime(10L), Times.Once());
}
