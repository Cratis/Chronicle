// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents the request for rewinding a partition.
/// </summary>
/// <param name="ObserverId">The observer to replay.</param>
/// <param name="MicroserviceId">The microservice the partition being replayed belongs to.</param>
/// <param name="EventSequenceId">From which event sequence the replay is happening for.</param>
/// <param name="TenantId">The tenant the partition being replayed belongs to.</param>
/// <param name="Partition">The partition being replayed.</param>
/// <param name="SequenceNumber">From which sequence number we're replaying.</param>
/// <param name="EventTypes">The event types for the replay.</param>
public record ReplayPartitionRequest(
    ObserverId ObserverId,
    MicroserviceId MicroserviceId,
    EventSequenceId EventSequenceId,
    TenantId TenantId,
    EventSourceId Partition,
    EventSequenceNumber SequenceNumber,
    IEnumerable<EventType> EventTypes);
