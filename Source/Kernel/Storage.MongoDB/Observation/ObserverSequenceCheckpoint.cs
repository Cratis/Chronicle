// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents a parked sequence-local observer checkpoint in MongoDB.
/// </summary>
public class ObserverSequenceCheckpoint
{
    /// <summary>Gets or sets the compound observer and sequence identifier.</summary>
    public required ObserverSequenceCheckpointId Id { get; set; }

    /// <summary>Gets or sets the next event to handle.</summary>
    public EventSequenceNumber NextEventSequenceNumber { get; set; } = EventSequenceNumber.First;

    /// <summary>Gets or sets the last handled event.</summary>
    public EventSequenceNumber LastHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>Gets or sets the observed tail.</summary>
    public EventSequenceNumber TailEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>Gets or sets the sequence's failed partition count.</summary>
    public FailedPartitionCount FailedPartitionCount { get; set; } = FailedPartitionCount.Zero;

    /// <summary>Gets or sets whether the sequence is replaying.</summary>
    public bool IsReplaying { get; set; }

    /// <summary>Gets or sets the sequence's partition claims, absent on legacy state.</summary>
    public IEnumerable<PartitionClaim>? PartitionClaims { get; set; }

    /// <summary>Gets or sets the sequence's observer-wide claim, absent on legacy state.</summary>
    public ObserverWideClaim? ObserverWideClaim { get; set; }

    /// <summary>Gets or sets the sequence's delivery fences, absent on legacy state.</summary>
    public IEnumerable<DeliveryFenceState>? DeliveryFences { get; set; }

    /// <summary>Gets or sets when this snapshot was parked.</summary>
    public DateTimeOffset Parked { get; set; }
}
