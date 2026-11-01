// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints;

/// <summary>
/// Represents a sequence-local observer checkpoint in SQL storage.
/// </summary>
public class ObserverSequenceCheckpointEntry
{
    /// <summary>Gets or sets the observer identifier.</summary>
    [MaxLength(200)]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>Gets or sets the sequence identifier.</summary>
    [MaxLength(200)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the next event to handle.</summary>
    public ulong NextEventSequenceNumber { get; set; }

    /// <summary>Gets or sets the last handled event.</summary>
    public ulong LastHandledEventSequenceNumber { get; set; }

    /// <summary>Gets or sets the observed tail.</summary>
    public ulong TailEventSequenceNumber { get; set; }

    /// <summary>Gets or sets the sequence's failed partition count.</summary>
    public int FailedPartitionCount { get; set; }

    /// <summary>Gets or sets whether the sequence is replaying.</summary>
    public bool IsReplaying { get; set; }

    /// <summary>Gets or sets the optional partition claims JSON.</summary>
    public string? PartitionClaims { get; set; }

    /// <summary>Gets or sets the optional observer-wide claim JSON.</summary>
    public string? ObserverWideClaim { get; set; }

    /// <summary>Gets or sets the optional delivery fences JSON.</summary>
    public string? DeliveryFences { get; set; }

    /// <summary>Gets or sets when this checkpoint was parked.</summary>
    public DateTimeOffset Parked { get; set; }
}
