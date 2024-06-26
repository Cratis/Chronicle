// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming.for_EventSequenceCache.given;

public class an_event_sequence_cache : Specification
{
    protected EventStoreName event_store_name;
    protected EventStoreNamespaceName event_store_namespace;
    protected Mock<IEventSequenceStorage> event_sequence_storage;

    protected EventSequenceCacheForSpecs cache;

    void Establish()
    {
        event_store_name = Guid.NewGuid().ToString();
        event_store_namespace = Guid.NewGuid().ToString();

        event_sequence_storage = new();

        event_sequence_storage.Setup(_ => _.GetTailSequenceNumber(null, null)).Returns(Task.FromResult(EventSequenceNumber.First));

        var cursor = new Mock<IEventCursor>();
        cursor.Setup(_ => _.MoveNext()).Returns(Task.FromResult(false));

        event_sequence_storage.Setup(_ =>
            _.GetRange(0, EventSequenceCache.NumberOfEventsToFetch, null, null, default))
            .Returns(Task.FromResult(cursor.Object));

        cache = new(
            event_sequence_storage.Object,
            Mock.Of<ILogger<EventSequenceCache>>());
    }
}
