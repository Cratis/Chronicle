// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reducers;

/// <summary>
/// Represents an event type.
/// </summary>
public class ReducerDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the event type.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier for the reactor.
    /// </summary>
    public string EventSequenceId { get; set; } = Concepts.EventSequences.EventSequenceId.Log;

    /// <summary>
    /// Gets or sets the event types the reactor is interested in.
    /// </summary>
    [Json]
    public IEnumerable<EventTypeWithKeyExpression> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the read model identifier.
    /// </summary>
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the sink for the projection.
    /// </summary>
    public Guid SinkType { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the configuration identifier for the sink of the projection.
    /// </summary>
    public Guid SinkConfigurationId { get; set; } = Guid.Empty;
}
