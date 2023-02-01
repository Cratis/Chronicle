// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;


public class an_event_sequence_queue_cache_cursor : all_dependencies
{
    protected EventSequenceQueueCacheCursor cursor;

    void Establish()
    {
        cache.Setup(_ => _.GetView(IsAny<EventSequenceNumber>(), IsAny<EventSequenceNumber>())).Returns(new SortedSet<AppendedEvent>());
        cursor = new(
            cache.Object,
            microservice_id,
            tenant_id,
            event_sequence_id,
            EventSequenceNumber.First);
    }
}
