// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.given;

public abstract class an_event_store_with_a_set_of_events : a_set_of_events
{
    protected EventSequenceCache cache;

    void Establish() => cache = new(event_sequence_id, range_size, storage_provider.Object);
}
