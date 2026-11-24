// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IReplayObserverPartition"/>.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="Key"><see cref="Key">Partition</see> to retry.</param>
/// <param name="FromSequenceNumber">From <see cref="EventSequenceNumber"/> to retry.</param>
/// <param name="ToSequenceNumber">To <see cref="EventSequenceNumber"/> to retry.</param>
/// <param name="EventTypes">The event types to replay.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.Jobs.ReplayObserverPartitionRequest")]
public record ReplayObserverPartitionRequest(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] ObserverType ObserverType,
    [Id(2)] Key Key,
    [Id(3)] EventSequenceNumber FromSequenceNumber,
    [Id(4)] EventSequenceNumber ToSequenceNumber,
    [Id(5)] IEnumerable<EventType> EventTypes) : ObserverPartitionedJobRequest(ObserverKey, ObserverType, Key)
{
    /// <summary>
    /// Gets whether this replay includes all event types, so a successfully replayed partition can resolve an old failure.
    /// </summary>
    [Id(6)]
    public bool ReplaysAllEventTypes { get; init; }

    /// <summary>
    /// Gets the first position withheld from live delivery when the partition entered replay.
    /// </summary>
    /// <remarks>Legacy persisted jobs have no safe bound and deserialize as unavailable; refusal then quarantines the partition.</remarks>
    [Id(7)]
    public EventSequenceNumber HeldBackFrom { get; init; } = EventSequenceNumber.Unavailable;
}
