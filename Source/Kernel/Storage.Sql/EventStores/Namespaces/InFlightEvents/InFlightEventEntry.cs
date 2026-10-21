// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents;

/// <summary>
/// Represents a persisted in-flight event for an observer partition.
/// </summary>
public class InFlightEventEntry
{
    /// <summary>
    /// Gets or sets the unique identifier of the entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the observer.
    /// </summary>
    [MaxLength(200)]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the partition the event belongs to.
    /// </summary>
    [MaxLength(200)]
    public string Partition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence number of the in-flight event.
    /// </summary>
    public decimal EventSequenceNumber { get; set; }
}
