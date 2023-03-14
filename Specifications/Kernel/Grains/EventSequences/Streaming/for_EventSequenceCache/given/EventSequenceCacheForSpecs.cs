// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
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

    public LinkedList<AppendedEvent> Events => _events;
    public Dictionary<EventSequenceNumber, LinkedListNode<AppendedEvent>> EventsBySequenceNumber => _eventsBySequenceNumber;
}
