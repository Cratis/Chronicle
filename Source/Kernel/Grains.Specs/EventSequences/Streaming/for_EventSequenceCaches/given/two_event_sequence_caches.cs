// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming.for_EventSequenceCaches.given;

public class two_event_sequence_caches : an_event_sequence_caches
{
    protected static readonly EventStoreName first_event_store = Guid.NewGuid().ToString();
    protected static readonly EventStoreNamespaceName first_namespace = Guid.NewGuid().ToString();
    protected static readonly EventSequenceId first_event_sequence_id = Guid.NewGuid();
    protected static readonly EventStoreName second_event_store = Guid.NewGuid().ToString();
    protected static readonly EventStoreNamespaceName second_namespace = Guid.NewGuid().ToString();
    protected static readonly EventSequenceId second_event_sequence_id = Guid.NewGuid();

    protected Mock<IEventSequenceCache> first_cache;
    protected Mock<IEventSequenceCache> second_cache;

    void Establish()
    {
        first_cache = new();
        second_cache = new();

        first_cache = new();
        second_cache = new();

        event_sequence_cache_factory.Setup(_ => _.Create(first_event_store, first_namespace, first_event_sequence_id)).Returns(first_cache.Object);
        event_sequence_cache_factory.Setup(_ => _.Create(second_event_store, second_namespace, second_event_sequence_id)).Returns(second_cache.Object);

        caches.GetFor(first_event_store, first_namespace, first_event_sequence_id);
        caches.GetFor(second_event_store, second_namespace, second_event_sequence_id);
    }
}
