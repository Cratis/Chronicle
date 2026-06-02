// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Events;

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
    public EventTypeId Type { get; set; } = EventTypeId.Unknown;

    /// <summary>
    /// Gets or sets the time the event occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the event source type.
    /// </summary>
    public EventSourceType EventSourceType { get; set; } = EventSourceType.Default;

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    public EventSourceId EventSourceId { get; set; } = EventSourceId.Unspecified;

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    public EventStreamType EventStreamType { get; set; } = EventStreamType.All;

    /// <summary>
    /// Gets or sets the event stream identifier.
    /// </summary>
    public EventStreamId EventStreamId { get; set; } = EventStreamId.Default;

    /// <summary>
    /// Gets or sets the content per event type generation.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject that identifies the compliance target for the event.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the compensations for the event.
    /// </summary>
    [Json]
    public IDictionary<string, string> Compensations { get; set; } = new Dictionary<string, string>();
}
