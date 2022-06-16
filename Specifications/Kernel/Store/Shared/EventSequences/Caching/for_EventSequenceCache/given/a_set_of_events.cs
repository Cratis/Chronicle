// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.given;

public abstract class a_set_of_events : all_dependencies
{
    protected abstract EventSequenceCacheRange range { get; }
    protected abstract int cursor_size { get; }
    protected abstract int range_size { get; }

    protected EventSequenceId event_sequence_id;

    void Establish()
    {
        event_sequence_id = EventSequenceId.Log;
        storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, null, null)).Returns(Task.FromResult(range.End));
        storage_provider.Setup(_ => _.GetRange(event_sequence_id, EventSequenceNumber.First, (ulong)range_size, null, null)).Returns(
            Task.FromResult<IEventCursor>(new FakeEventCursor(range.Start, range.End, cursor_size)));
    }
}
