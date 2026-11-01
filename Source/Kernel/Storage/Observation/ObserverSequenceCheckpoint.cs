// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the sequence-local progress parked when an observer changes its owning sequence.
/// Observer-wide policy, alert lifecycle and handled counts remain on the observer state.
/// </summary>
/// <param name="ObserverId">The observer whose progress is parked.</param>
/// <param name="EventSequenceId">The sequence the progress belongs to.</param>
/// <param name="NextEventSequenceNumber">The next event to handle.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event.</param>
/// <param name="TailEventSequenceNumber">The observed tail.</param>
/// <param name="FailedPartitionCount">The number of failures on this sequence.</param>
/// <param name="IsReplaying">Whether this sequence is replaying.</param>
/// <param name="PartitionClaims">The sequence's partition claims, absent on legacy state.</param>
/// <param name="ObserverWideClaim">The sequence's observer-wide claim, absent on legacy state.</param>
/// <param name="DeliveryFences">The sequence's delivery fences, absent on legacy state.</param>
/// <param name="Parked">When this snapshot was parked.</param>
public record ObserverSequenceCheckpoint(
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    EventSequenceNumber NextEventSequenceNumber,
    EventSequenceNumber LastHandledEventSequenceNumber,
    EventSequenceNumber TailEventSequenceNumber,
    FailedPartitionCount FailedPartitionCount,
    bool IsReplaying,
    IEnumerable<PartitionClaim>? PartitionClaims,
    ObserverWideClaim? ObserverWideClaim,
    IEnumerable<DeliveryFenceState>? DeliveryFences,
    DateTimeOffset Parked)
{
    /// <summary>
    /// Creates the initial progress for a sequence this observer has never owned.
    /// </summary>
    /// <param name="observerId">The observer.</param>
    /// <param name="eventSequenceId">The sequence.</param>
    /// <returns>A checkpoint starting at the first event, without claims or failures.</returns>
    public static ObserverSequenceCheckpoint Fresh(ObserverId observerId, EventSequenceId eventSequenceId) =>
        new(
            observerId,
            eventSequenceId,
            EventSequenceNumber.First,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable,
            FailedPartitionCount.Zero,
            false,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);

    /// <summary>
    /// Takes only the sequence-local progress from an observer state.
    /// </summary>
    /// <param name="state">The state to park.</param>
    /// <param name="eventSequenceId">The sequence that owns this progress.</param>
    /// <returns>The parked sequence-local snapshot.</returns>
    public static ObserverSequenceCheckpoint ParkedFrom(ObserverState state, EventSequenceId eventSequenceId) =>
        new(
            state.Identifier,
            eventSequenceId,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber,
            state.TailEventSequenceNumber,
            state.FailedPartitionCount,
            state.IsReplaying,
            state.PartitionClaims,
            state.ObserverWideClaim,
            state.DeliveryFences,
            DateTimeOffset.UtcNow);
}
