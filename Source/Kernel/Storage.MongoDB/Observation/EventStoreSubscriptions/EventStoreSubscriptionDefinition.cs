// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the MongoDB storage document for an event store subscription definition.
/// </summary>
public class EventStoreSubscriptionDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the subscription.
    /// </summary>
    [BsonId]
    public EventStoreSubscriptionId Id { get; set; } = EventStoreSubscriptionId.Unspecified;

    /// <summary>
    /// Gets or sets the source event store name.
    /// </summary>
    public EventStoreName SourceEventStore { get; set; } = EventStoreName.NotSet;

    /// <summary>
    /// Gets or sets the event types to subscribe to.
    /// </summary>
    public IList<EventType> EventTypes { get; set; } = [];
}
