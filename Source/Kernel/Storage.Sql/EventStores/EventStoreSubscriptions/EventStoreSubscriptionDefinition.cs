// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions;

/// <summary>
/// Represents the SQL storage entity for an event store subscription definition.
/// </summary>
public class EventStoreSubscriptionDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the subscription.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the source event store name.
    /// </summary>
    public string SourceEventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event types the subscription covers.
    /// </summary>
    [Json]
    public IEnumerable<EventType> EventTypes { get; set; } = [];
}
