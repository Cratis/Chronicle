// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.given;

public class a_supervisor : Specification
{
    protected static readonly MicroserviceId microservice_id = Guid.NewGuid();
    protected static readonly TenantId tenant_id = Guid.NewGuid();
    protected static readonly EventSequenceId event_sequence_id = Guid.NewGuid();
    protected static readonly IEnumerable<EventType> event_types = new[] { new EventType(Guid.NewGuid(), 1, false), new EventType(Guid.NewGuid(), 1, false) };
    protected static readonly ObserverId observer_id = Guid.NewGuid();

    protected static readonly ObserverKey observer_key =
        new(microservice_id, tenant_id, event_sequence_id, null, null);

    protected static Mock<IGrainFactory> grain_factory_mock = new();

    protected FailedPartitionSupervisor get_clean_supervisor() =>
        new(observer_id, observer_key, event_types, null, grain_factory_mock.Object);

    protected FailedPartitionSupervisor get_supervisor_with_failed_partition(EventSourceId partition, EventSequenceNumber? fromEvent = null)
    {
        var failedPartitions = new FailedPartition[] { new(partition, fromEvent ?? EventSequenceNumber.First, Enumerable.Empty<string>(), string.Empty, DateTimeOffset.UtcNow) };
        return new(observer_id, observer_key, event_types, failedPartitions, grain_factory_mock.Object);
    }

    protected PartitionedObserverKey get_partitioned_observer_key(EventSourceId partition_key) =>
        PartitionedObserverKey.FromObserverKey(observer_key, partition_key);

    protected Mock<IRecoverFailedPartition> a_failed_partition_mock(EventSourceId partition_key) => new();
}
