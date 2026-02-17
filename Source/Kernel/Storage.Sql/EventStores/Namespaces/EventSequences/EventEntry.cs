// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents the entity for individual events in the event sequence.
/// </summary>
public class EventEntry
{
    /// <summary>
    /// Gets or sets the sequence number of the event - the primary key.
    /// </summary>
    [Key]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized causation chain.
    /// </summary>
    public string Causation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized caused by chain.
    /// </summary>
    public string CausedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type identifier.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time the event occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the event source type.
    /// </summary>
    public string EventSourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    public string EventStreamType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream identifier.
    /// </summary>
    public string EventStreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content per event type generation.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the compensations for the event.
    /// </summary>
    [Json]
    public IDictionary<string, string> Compensations { get; set; } = new Dictionary<string, string>();
}
