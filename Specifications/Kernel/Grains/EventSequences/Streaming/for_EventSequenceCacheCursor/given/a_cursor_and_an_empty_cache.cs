// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;

public class a_cursor_and_an_empty_cache : all_dependencies
{
    protected EventSequenceQueueCacheCursor cursor;

    void Establish()
    {
        cursor = new EventSequenceQueueCacheCursor(
            cache.Object,
            event_store_name,
            event_store_namespace,
            event_sequence_id,
            EventSequenceNumber.First);
    }
}
