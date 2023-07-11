// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCaches.given;

public class two_event_sequence_caches : an_event_sequence_caches
{
    protected static readonly MicroserviceId first_microservice_id = Guid.NewGuid();
    protected static readonly TenantId first_tenant_id = Guid.NewGuid();
    protected static readonly EventSequenceId first_event_sequence_id = Guid.NewGuid();
    protected static readonly MicroserviceId second_microservice_id = Guid.NewGuid();
    protected static readonly TenantId second_tenant_id = Guid.NewGuid();
    protected static readonly EventSequenceId second_event_sequence_id = Guid.NewGuid();

    protected Mock<IEventSequenceCache> first_cache;
    protected Mock<IEventSequenceCache> second_cache;

    void Establish()
    {
        first_cache = new();
        second_cache = new();

        first_cache = new();
        second_cache = new();

        event_sequence_cache_factory.Setup(_ => _.Create(first_microservice_id, first_tenant_id, first_event_sequence_id)).Returns(first_cache.Object);
        event_sequence_cache_factory.Setup(_ => _.Create(second_microservice_id, second_tenant_id, second_event_sequence_id)).Returns(second_cache.Object);

        caches.GetFor(first_microservice_id, first_tenant_id, first_event_sequence_id);
        caches.GetFor(second_microservice_id, second_tenant_id, second_event_sequence_id);
    }
}
