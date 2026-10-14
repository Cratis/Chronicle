// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

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
    readonly HashSet<EventType> _declaredEventTypes = new();
    EventGenerationDelivery _generationDelivery = EventGenerationDelivery.Compatibility;

    /// <inheritdoc/>
    public IEventStoreSubscriptionBuilder WithGenerationDelivery(EventGenerationDelivery generationDelivery)
    {
        _generationDelivery = generationDelivery;
        return this;
    }

    /// <inheritdoc/>
    public IEventStoreSubscriptionBuilder WithEventType<TEvent>()
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEvent));
        _eventTypes.Add(eventType.Id);
        _declaredEventTypes.Add(eventType);
        return this;
    }

    /// <inheritdoc/>
    public IEventStoreSubscriptionBuilder WithEventType(EventTypeId eventTypeId)
    {
        _eventTypes.Add(eventTypeId);
        return this;
    }

    /// <inheritdoc/>
    public IEventStoreSubscriptionBuilder WithEventType(EventTypeId eventTypeId, EventTypeGeneration generation)
    {
        _eventTypes.Add(eventTypeId);
        _declaredEventTypes.Add(new(eventTypeId, generation));
        return this;
    }

    /// <inheritdoc/>
    public EventStoreSubscriptionDefinition Build()
    {
        var ids = _eventTypes.Count > 0 ? _eventTypes : eventTypes.All.Select(et => et.Id);
        var pins = _generationDelivery == EventGenerationDelivery.Pinned ? ResolvePins(ids) : _declaredEventTypes.ToArray();
        return new(subscriptionId, sourceEventStore, _generationDelivery == EventGenerationDelivery.Pinned ? ids.ToArray() : ids)
        {
            GenerationDelivery = _generationDelivery,
            EventTypePins = pins
        };
    }

    EventType[] ResolvePins(IEnumerable<EventTypeId> ids)
    {
        var pins = new List<EventType>();
        foreach (var id in ids.Distinct())
        {
            var declared = _declaredEventTypes.Where(_ => _.Id == id).ToArray();
            var candidates = declared.Length > 0 ? declared : eventTypes.All.Where(_ => _.Id == id).Distinct().ToArray();
            if (candidates.Select(_ => _.Generation).Distinct().Count() > 1)
            {
                throw new MultipleGenerationsOfEventTypeInObserver(id);
            }

            if (candidates.Length == 0)
            {
                throw new EventStoreSubscriptionGenerationNotResolved(id);
            }

            pins.Add(candidates[0]);
        }

        if (pins.Count == 0)
        {
            throw new EventStoreSubscriptionGenerationNotResolved(EventTypeId.Unknown);
        }

        return pins.ToArray();
    }
}
