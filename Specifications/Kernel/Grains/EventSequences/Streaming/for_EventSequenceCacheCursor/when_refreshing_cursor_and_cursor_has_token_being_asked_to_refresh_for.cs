// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor;

public class when_refreshing_cursor_and_cursor_has_token_being_asked_to_refresh_for : given.cursor_with_ten_events_in_cache
{
    void Because() => cursor.Refresh(new EventSequenceNumberToken(5L));

    [Fact] void should_not_prime_cache() => cache.Verify(_ => _.Prime(5L), Never);
}
