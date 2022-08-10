// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.given;

public class no_events_in_sequence : all_dependencies
{
    protected EventSequenceCache cache;
    protected EventSequenceId event_sequence_id;
    protected int cursor_size;
    protected int range_size;

    void Establish()
    {
        event_sequence_id = EventSequenceId.Log;
        cursor_size = 10;
        range_size = 100;

        storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, null, null)).Returns(Task.FromResult((EventSequenceNumber)0));
        storage_provider
            .Setup(_ => _.GetRange(event_sequence_id, EventSequenceNumber.First, (ulong)range_size - 1, null, null))
            .Returns(Task.FromResult<IEventCursor>(new FakeEventCursor(0, 0, cursor_size)));

        cache = new EventSequenceCache(event_sequence_id, 100, storage_provider.Object);
    }
}
