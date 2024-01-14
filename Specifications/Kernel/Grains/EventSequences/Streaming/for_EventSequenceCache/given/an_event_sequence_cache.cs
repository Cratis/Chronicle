// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.EventSequences;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache.given;

public class an_event_sequence_cache : Specification
{
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IEventSequenceStorage> event_sequence_storage;

    protected EventSequenceCacheForSpecs cache;

    void Establish()
    {
        microservice_id = Guid.NewGuid();
        tenant_id = Guid.NewGuid();

        execution_context_manager = new();
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
