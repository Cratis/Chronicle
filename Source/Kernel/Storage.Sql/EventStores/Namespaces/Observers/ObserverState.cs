// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Represents an event type.
/// </summary>
public class ObserverState
{
    /// <summary>
    /// Gets or sets the unique identifier for the observer.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the last event the observer handled.
    /// </summary>
    public ulong LastHandledEventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the running state of the observer.
    /// </summary>
    public ObserverRunningState RunningState { get; set; } = ObserverRunningState.Unknown;

    /// <summary>
    /// Gets or sets the individual partitions that are currently being replayed.
    /// </summary>
    [Json]
    public ISet<string> ReplayingPartitions { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the individual partitions that are catching up.
    /// </summary>
    [Json]
    public ISet<string> CatchingUpPartitions { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the collection of <see cref="FailedPartition"/> associated with the observer.
    /// </summary>
    [Json]
    public IEnumerable<FailedPartition> FailedPartitions { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the observer is currently replaying events.
    /// </summary>
    public bool IsReplaying { get; set; }
}
