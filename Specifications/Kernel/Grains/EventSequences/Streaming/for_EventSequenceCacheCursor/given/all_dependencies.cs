// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;

public class all_dependencies : Specification
{
    protected Mock<IEventSequenceCache> cache;
    protected EventStoreName event_store_name;
    protected EventStoreNamespaceName event_store_namespace;
    protected EventSequenceId event_sequence_id;

    void Establish()
    {
        cache = new();
        event_store_name = Guid.NewGuid().ToString();
        event_store_namespace = Guid.NewGuid().ToString();
        event_sequence_id = Guid.NewGuid();
    }
}
