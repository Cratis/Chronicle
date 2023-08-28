// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

public class EventSequenceCacheForSpecs : EventSequenceCache
{
    public EventSequenceCacheForSpecs(
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId,
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        ILogger<EventSequenceCache> logger) : base(microserviceId, tenantId, eventSequenceId, executionContextManager, eventSequenceStorageProvider, logger)
    {
    }

    public IEnumerable<AppendedEvent> Events => _eventsBySequenceNumber.Select(_ => _.Value.Event);
    public CachedAppendedEvent HeadEvent => _head;
    public CachedAppendedEvent TailEvent => _tail;
    public Dictionary<EventSequenceNumber, CachedAppendedEvent> EventsBySequenceNumber => _eventsBySequenceNumber;
}
