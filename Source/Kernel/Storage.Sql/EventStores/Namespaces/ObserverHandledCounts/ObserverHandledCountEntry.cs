// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts;

/// <summary>
/// Represents the persisted handled-event count for an observer, partition and event type.
/// </summary>
public class ObserverHandledCountEntry
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
    /// Gets or sets the partition the count is for.
    /// </summary>
    [MaxLength(200)]
    public string Partition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the event type.
    /// </summary>
    [MaxLength(200)]
    public string EventTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of handled events.
    /// </summary>
    public long Count { get; set; }
}
