// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IReplayObserver"/>.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="FromEventSequenceNumber">The <see cref="EventSequenceNumber"/> it should catch up from.</param>
/// <param name="EventTypes">The event types to replay.</param>
public record CatchUpObserverRequest(
    ObserverKey ObserverKey,
    ObserverType ObserverType,
    EventSequenceNumber FromEventSequenceNumber,
    IEnumerable<EventType> EventTypes) : IObserverJobRequest
{
    /// <summary>
    /// Gets the partitions left behind by an earlier catch-up, each read from its own position, when this catch-up only
    /// finishes what that one left behind. Empty for an ordinary catch-up over every partition.
    /// </summary>
    public IEnumerable<CatchUpObserverPartitionRange> PartitionsLeftBehind { get; init; } = [];

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> an earlier catch-up got every other partition to, when this catch-up
    /// only finishes what that one left behind. The partitions left behind are read up to and including it, and the
    /// observer is told it has caught up at least that far.
    /// </summary>
    public EventSequenceNumber ToEventSequenceNumber { get; init; } = EventSequenceNumber.Max;
}
