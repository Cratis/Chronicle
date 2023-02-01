// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache.given;

public class an_event_sequence_cache : Specification
{
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected EventSequenceId event_sequence_id;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IEventSequenceStorageProvider> event_sequence_storage_provider;

    protected EventSequenceCacheForSpecs cache;

    void Establish()
    {
        microservice_id = Guid.NewGuid();
        tenant_id = Guid.NewGuid();
        event_sequence_id = Guid.NewGuid();

        execution_context_manager = new();
        event_sequence_storage_provider = new();

        cache = new(
            microservice_id,
            tenant_id,
            event_sequence_id,
            execution_context_manager.Object,
            () => event_sequence_storage_provider.Object,
            Mock.Of<ILogger<EventSequenceCache>>());
    }
}
