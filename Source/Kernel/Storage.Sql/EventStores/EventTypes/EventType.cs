// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary - false positive, all directives are needed
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

/// <summary>
/// Represents an event type.
/// </summary>
public class EventType
{
    /// <summary>
    /// Gets or sets the unique identifier for the event type.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the owner identifier for the event type.
    /// </summary>
    public EventTypeOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets the source identifier for the event type.
    /// </summary>
    public EventTypeSource Source { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the event type is a tombstone.
    /// </summary>
    public bool Tombstone { get; set; }

    /// <summary>
    /// Gets or sets the versioned schemas associated with the event type.
    /// </summary>
    [Json]
    public IDictionary<uint, string> Schemas { get; set; } = new Dictionary<uint, string>();
}
