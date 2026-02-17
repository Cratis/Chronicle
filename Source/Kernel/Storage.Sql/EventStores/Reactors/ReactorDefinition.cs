// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reactors;

/// <summary>
/// Represents an event type.
/// </summary>
public class ReactorDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the event type.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the owner identifier for the event type.
    /// </summary>
    public ReactorOwner Owner { get; set; }

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
    /// Gets or sets a value indicating whether the reactor is replayable.
    /// </summary>
    public bool IsReplayable { get; set; } = true;
}
