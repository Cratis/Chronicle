// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A durable partition replay completion and its catch-up handoff.
/// </summary>
/// <param name="Partition">The replayed partition.</param>
/// <param name="JobId">The completed job, or null for legacy callbacks.</param>
public record PartitionReplayCompletionState(Key Partition, JobId? JobId)
{
    /// <summary>
    /// Gets the last event handled.
    /// </summary>
    public EventSequenceNumber LastHandledEventSequenceNumber { get; init; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets the last event scanned.
    /// </summary>
    public EventSequenceNumber LastScannedEventSequenceNumber { get; init; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets the event types covered by the replay.
    /// </summary>
    public IReadOnlyList<EventType> ReplayedEventTypes { get; init; } = [];

    /// <summary>
    /// Gets whether the replay position is committed and catch-up is owed.
    /// </summary>
    public bool HandingOff { get; init; }

    /// <summary>
    /// Gets whether catch-up was handed off. The job identity remains until job storage confirms finalization.
    /// </summary>
    public bool HandedOff { get; init; }
}
