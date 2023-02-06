// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;

public class an_empty_cursor_and_empty_cache : an_empty_cache
{
    protected EventSequenceQueueCacheCursor cursor;

    void Establish()
    {
        cursor = new EventSequenceQueueCacheCursor(
            cache.Object,
            microservice_id,
            tenant_id,
            event_sequence_id,
            EventSequenceNumber.First);
    }
}
