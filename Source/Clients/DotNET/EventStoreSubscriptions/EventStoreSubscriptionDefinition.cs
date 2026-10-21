// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Represents a definition of an event store subscription.
/// </summary>
/// <param name="Id">The unique identifier for the subscription.</param>
/// <param name="SourceEventStore">The name of the source event store to subscribe to.</param>
/// <param name="EventTypes">The event types to subscribe to.</param>
public record EventStoreSubscriptionDefinition(
    EventStoreSubscriptionId Id,
    string SourceEventStore,
    IEnumerable<EventTypeId> EventTypes)
{
    /// <summary>
    /// Gets the generation delivery policy. Compatibility is the default.
    /// </summary>
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;

    /// <summary>
    /// Gets the exact event generations declared for pinned delivery.
    /// </summary>
    public IEnumerable<EventType> EventTypePins { get; init; } = [];
}
