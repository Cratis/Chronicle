// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the request for rewinding a partition.
/// </summary>
/// <param name="EventStore">Event store that is affected.</param>
/// <param name="Namespace">Namespace that is affected.</param>
/// <param name="EventSequenceId">The event sequence the event belongs to.</param>
/// <param name="EventSourceId">The event source id of the redaction.</param>
/// <param name="SequenceNumber">The sequence number to rewind to.</param>
/// <param name="AffectedEventTypes">Affected event types.</param>
public record RewindPartitionForObserversAfterRedactRequest(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    EventSequenceNumber SequenceNumber,
    IEnumerable<EventType> AffectedEventTypes);
