// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor;

public class when_refreshing_cursor_and_cache_does_not_have_token_being_asked_to_refresh_for : given.a_cursor_and_an_empty_cache
{
    void Establish() => cache.Setup(_ => _.HasEvent(5L)).Returns(false);

    void Because() => cursor.Refresh(new EventSequenceNumberToken(5L));

    [Fact] void should_not_prime_cache() => cache.Verify(_ => _.Prime(5L), Once);
}
