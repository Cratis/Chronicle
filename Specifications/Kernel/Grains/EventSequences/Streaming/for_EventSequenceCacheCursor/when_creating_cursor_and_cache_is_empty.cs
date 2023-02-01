// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor;

public class when_creating_cursor_and_cache_is_empty : given.an_empty_cache
{
    EventSequenceQueueCacheCursor cursor;

    void Because() =>
        cursor = new EventSequenceQueueCacheCursor(
            cache.Object,
            microservice_id,
            tenant_id,
            event_sequence_id,
            EventSequenceNumber.First);

    [Fact] void should_prime_cache() => cache.Verify(_ => _.Prime(EventSequenceNumber.First), Once);
}
