// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers;

/// <summary>
/// Represents an event type.
/// </summary>
public class ObserverDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the observer.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the event types the observer is interested in.
    /// </summary>
    [Json]
    public IEnumerable<EventTypeWithKeyExpression> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the event sequence identifier for the reactor.
    /// </summary>
    public string EventSequenceId { get; set; } = Concepts.EventSequences.EventSequenceId.Log;

    /// <summary>
    /// Gets or sets the observer type.
    /// </summary>
    public ObserverType Type { get; set; } = ObserverType.Unknown;

    /// <summary>
    /// Gets or sets the owner identifier for the observer.
    /// </summary>
    public ObserverOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the observer is replayable.
    /// </summary>
    public bool IsReplayable { get; set; } = true;
}
