// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Selects and releases subscription generations, caching registration data for one forwarding page.
/// </summary>
/// <param name="source">The source event type storage.</param>
/// <param name="target">The target event type storage.</param>
/// <param name="release">The generation release service.</param>
/// <param name="sourceStore">The source event store.</param>
/// <param name="targetStore">The target event store.</param>
/// <param name="sourceSequence">The source sequence containing protected event history.</param>
public class EventStoreSubscriptionTargetGeneration(IEventTypesStorage source, IEventTypesStorage target, IEventGenerationRelease release, EventStoreName sourceStore, EventStoreName targetStore, IEventSequenceStorage sourceSequence)
{
    readonly Dictionary<EventTypeId, EventTypeGeneration[]> _generations = [];
    readonly Dictionary<EventType, EventTypeSchema> _schemas = [];

    /// <summary>
    /// Releases a pin or the highest registered target generation no newer than the event.
    /// </summary>
    /// <param name="incoming">The source event.</param>
    /// <param name="pins">The subscription's explicit generation pins.</param>
    /// <returns>The event ready for the target.</returns>
    /// <exception cref="EventStoreSubscriptionGenerationUnavailable">No registered generation can be delivered.</exception>
    public async Task<AppendedEvent> Release(AppendedEvent incoming, IReadOnlyDictionary<EventTypeId, EventType> pins)
    {
        var eventType = incoming.Context.EventType;
        var generations = await GetGenerations(eventType.Id);
        if (pins.Count == 0 && generations.Length == 0) return incoming;
        try
        {
            var selected = SelectForSubscription(eventType, pins, generations);
            await EventStoreSubscriptionGenerationValidation.GetExactSchema(target, selected);

            // The observer has already released this generation; never decrypt its plaintext again.
            if (selected.Generation == eventType.Generation) return incoming;

            if (!_schemas.ContainsKey(selected))
            {
                _schemas[selected] = await EventStoreSubscriptionGenerationValidation.GetExactSchema(source, selected);
            }

            // History on incoming is already plaintext. Re-select only from protected storage.
            var stored = await sourceSequence.GetEventAt(incoming.Context.SequenceNumber);
            var released = await release.Release(sourceStore, [selected], _schemas, [stored]);
            return released.Single();
        }
        catch (Exception exception) when (exception is EventTypeGenerationNotRegisteredForObserver or PinnedEventTypeGenerationUnavailable)
        {
            throw new EventStoreSubscriptionGenerationUnavailable(eventType, targetStore, generations, exception);
        }
    }

    static EventType SelectForSubscription(EventType eventType, IReadOnlyDictionary<EventTypeId, EventType> pins, EventTypeGeneration[] generations)
    {
        // Redaction markers have their own kernel schema and no content to migrate.
        if (eventType.Id == GlobalEventTypes.Redaction) return eventType;
        if (pins.Count == 0) return Select(eventType, generations);
        return pins.TryGetValue(eventType.Id, out var pin) ? pin : throw new PinnedEventTypeGenerationUnavailable(eventType);
    }

    static EventType Select(EventType sourceType, EventTypeGeneration[] generations)
    {
        var generation = generations.Where(_ => _.Value <= sourceType.Generation.Value).OrderByDescending(_ => _.Value).FirstOrDefault();
        return generation is null
            ? throw new EventTypeGenerationNotRegisteredForObserver(sourceType)
            : new(sourceType.Id, generation);
    }

    async Task<EventTypeGeneration[]> GetGenerations(EventTypeId id)
    {
        if (!_generations.TryGetValue(id, out var generations))
        {
            // This query returns no schemas for missing types, independently of any default generation.
            var schemas = await target.GetAllGenerationsForEventType(new(id, EventTypeGeneration.First));
            generations = schemas.Select(_ => _.Type.Generation).ToArray();
            _generations[id] = generations;
        }
        return generations;
    }
}
