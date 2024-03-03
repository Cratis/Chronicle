// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;

namespace Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;

public class all_dependencies : Specification
{
    protected Mock<IEventSequenceCache> cache;
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected EventSequenceId event_sequence_id;

    void Establish()
    {
        cache = new();
        microservice_id = Guid.NewGuid();
        tenant_id = Guid.NewGuid();
        event_sequence_id = Guid.NewGuid();
    }
}
