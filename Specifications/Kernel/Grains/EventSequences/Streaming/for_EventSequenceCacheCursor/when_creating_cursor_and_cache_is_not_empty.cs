// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor;

public class when_creating_cursor_and_cache_is_not_empty : given.cursor_with_ten_events_from_cache
{
    [Fact] void should_not_prime_cache() => cache.Verify(_ => _.Prime(EventSequenceNumber.First), Never);
}
