// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class when_refreshing_and_cache_has_the_event : given.a_cursor_and_an_empty_cache
{
    void Establish() => cache.Setup(_ => _.HasEvent(EventSequenceNumber.First)).Returns(true);

    void Because() => cursor.Refresh(new EventSequenceNumberToken(EventSequenceNumber.First));

    [Fact] void should_prime_cache_for_second_event() => cache.Verify(_ => _.Prime(EventSequenceNumber.First), Never);
}
