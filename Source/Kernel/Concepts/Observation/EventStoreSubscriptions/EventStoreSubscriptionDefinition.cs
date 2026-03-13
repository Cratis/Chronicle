// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the definition of an event store subscription.
/// </summary>
/// <param name="Identifier">The unique <see cref="EventStoreSubscriptionId"/> for the subscription.</param>
/// <param name="SourceEventStore">The source <see cref="EventStoreName"/> whose outbox to subscribe to.</param>
/// <param name="EventTypes">The event types to subscribe to.</param>
public record EventStoreSubscriptionDefinition(
    EventStoreSubscriptionId Identifier,
    EventStoreName SourceEventStore,
    IEnumerable<EventType> EventTypes);
