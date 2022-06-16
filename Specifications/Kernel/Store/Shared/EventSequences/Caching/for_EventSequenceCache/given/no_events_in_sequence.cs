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
        cache = new EventSequenceCache(event_sequence_id, 100, storage_provider.Object);
    }
}
