// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.given;

public class no_events_in_sequence : all_dependencies
{
    protected EventSequenceCache cache;
    protected EventSequenceId event_sequence_id;

    void Establish()
    {
        event_sequence_id = EventSequenceId.Log;
        storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, null, null)).Returns(Task.FromResult((EventSequenceNumber)0));
        storage_provider
            .Setup(_ => _.GetRange(event_sequence_id, EventSequenceNumber.First, (ulong)100 - 1, null, null))
            .Returns(Task.FromResult<IEventCursor>(new FakeEventCursor(0, 0, 10)));

        cache = new EventSequenceCache(event_sequence_id, 100, storage_provider.Object);
    }
}
