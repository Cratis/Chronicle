// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents the request for rewinding a partition.
/// </summary>
/// <param name="MicroserviceId">Microservice that is affected.</param>
/// <param name="TenantId">TenantId that is affected.</param>
/// <param name="EventSequenceId">The event sequence the event belongs to.</param>
/// <param name="EventSourceId">The event source id of the redaction.</param>
/// <param name="SequenceNumber">The sequence number to rewind to.</param>
/// <param name="AffectedEventTypes">Affected event types.</param>
public record RewindPartitionForObserversAfterRedactRequest(
    MicroserviceId MicroserviceId,
    TenantId TenantId,
    EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    EventSequenceNumber SequenceNumber,
    IEnumerable<EventType> AffectedEventTypes);
