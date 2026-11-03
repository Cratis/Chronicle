// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what a batch method knows about the page of events it is called for.
/// </summary>
/// <param name="ObserverId">The <see cref="Observation.ObserverId"/> of the observer.</param>
/// <param name="EventStore">The <see cref="EventStoreName"/> the events come from.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the events come from.</param>
/// <param name="EventSequenceId">The <see cref="EventSequences.EventSequenceId"/> the events come from.</param>
/// <param name="Origin">The <see cref="ObservationOrigin"/> of the page.</param>
/// <param name="Partitions">The partitions in the page.</param>
/// <param name="EventCount">The number of events in the page.</param>
/// <param name="FirstSequenceNumber">The sequence number of the first event in the page.</param>
/// <param name="LastSequenceNumber">The sequence number of the last event in the page.</param>
/// <param name="Deadline">When the kernel stops waiting for the result, or null when it waits without a deadline.</param>
/// <param name="CancellationToken">The <see cref="System.Threading.CancellationToken"/> that is cancelled when the client shuts down.</param>
public record BatchContext(
    ObserverId ObserverId,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId,
    ObservationOrigin Origin,
    IReadOnlyList<EventSourceId> Partitions,
    int EventCount,
    EventSequenceNumber FirstSequenceNumber,
    EventSequenceNumber LastSequenceNumber,
    DateTimeOffset? Deadline,
    CancellationToken CancellationToken)
{
    /// <summary>
    /// Gets a value indicating whether the page is part of a replay.
    /// </summary>
    public bool IsReplay => Origin == ObservationOrigin.Replay;
}
