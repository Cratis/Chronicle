// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class when_refreshing_and_cache_does_not_have_the_event : given.a_cursor_and_an_empty_cache
{
    void Establish() => cache.Setup(_ => _.HasEvent(EventSequenceNumber.First)).Returns(false);

    void Because() => cursor.Refresh(new EventSequenceNumberToken(EventSequenceNumber.First));

    [Fact] void should_prime_cache_for_second_event() => cache.Verify(_ => _.Prime(EventSequenceNumber.First), Once);
}
