// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;

public class cursor_with_ten_events_from_cache : all_dependencies
{
    protected AppendedEvent[] events;
    protected EventSequenceQueueCacheCursor cursor;

    void Establish()
    {
        events = Enumerable.Range(0, 10).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToArray();
        var sortedEvents = new SortedSet<AppendedEvent>(events, new AppendedEventComparer());
        cache.Setup(_ => _.GetView(IsAny<EventSequenceNumber>(), IsAny<EventSequenceNumber>())).Returns(sortedEvents);

        cursor = new EventSequenceQueueCacheCursor(
            cache.Object,
            microservice_id,
            tenant_id,
            event_sequence_id,
            EventSequenceNumber.First);
    }
}
