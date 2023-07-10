// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.given;

public class a_supervisor : Specification
{
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected EventSequenceId event_sequence_id;
    protected IEnumerable<EventType> event_types = new[] { new EventType(Guid.NewGuid(), 1, false), new EventType(Guid.NewGuid(), 1, false) };
    protected ObserverId observer_id;
    protected ObserverKey observer_key;

    protected Mock<IGrainFactory> grain_factory = new();

    void Establish()
    {
        microservice_id = Guid.NewGuid();
        tenant_id = Guid.NewGuid();
        event_sequence_id = Guid.NewGuid();
        observer_id = Guid.NewGuid();
        observer_key = new(microservice_id, tenant_id, event_sequence_id, null, null);
        grain_factory = new();
    }

    protected FailedPartitionSupervisor get_clean_supervisor() =>
        new(observer_id, observer_key, string.Empty, event_types, null, grain_factory.Object);

    protected FailedPartitionSupervisor get_supervisor_with_failed_partition(EventSourceId partition, EventSequenceNumber? fromEvent = null)
    {
        var failedPartitions = new FailedPartition[] { new(partition, fromEvent ?? EventSequenceNumber.First, Enumerable.Empty<string>(), string.Empty, DateTimeOffset.UtcNow) };
        return new(observer_id, observer_key, string.Empty, event_types, failedPartitions, grain_factory.Object);
    }

    protected PartitionedObserverKey get_partitioned_observer_key(EventSourceId partition_key) =>
        PartitionedObserverKey.FromObserverKey(observer_key, partition_key);

    protected Mock<IRecoverFailedPartition> a_failed_partition_mock(EventSourceId partition_key) => new();
}
