// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the event for an event store subscription that has been added.
/// </summary>
/// <param name="SourceEventStore">The source event store whose outbox to subscribe to.</param>
/// <param name="EventTypes">The event types the subscription observes.</param>
[EventType, AllEventStores]
public record EventStoreSubscriptionAdded(
    EventStoreName SourceEventStore,
    IEnumerable<EventType> EventTypes)
{
    /// <summary>
    /// Gets the generation delivery policy for this registration.
    /// </summary>
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;
}
