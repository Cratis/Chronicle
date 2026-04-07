// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreSubscriptionBuilder"/>.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for discovering event type identifiers.</param>
/// <param name="subscriptionId">The unique <see cref="EventStoreSubscriptionId"/> for this subscription.</param>
/// <param name="sourceEventStore">The name of the source event store to subscribe to.</param>
public class EventStoreSubscriptionBuilder(IEventTypes eventTypes, EventStoreSubscriptionId subscriptionId, string sourceEventStore) : IEventStoreSubscriptionBuilder
{
    readonly HashSet<EventTypeId> _eventTypes = new();

    /// <inheritdoc/>
    public IEventStoreSubscriptionBuilder WithEventType<TEvent>()
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEvent));
        _eventTypes.Add(eventType.Id);
        return this;
    }

    /// <inheritdoc/>
    public IEventStoreSubscriptionBuilder WithEventType(EventTypeId eventTypeId)
    {
        _eventTypes.Add(eventTypeId);
        return this;
    }

    /// <inheritdoc/>
    public EventStoreSubscriptionDefinition Build() =>
        new(
            subscriptionId,
            sourceEventStore,
            _eventTypes.Count > 0 ? _eventTypes : eventTypes.All.Select(et => et.Id));
}
