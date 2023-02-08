// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;

public class a_cursor_and_an_empty_cache : all_dependencies
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
