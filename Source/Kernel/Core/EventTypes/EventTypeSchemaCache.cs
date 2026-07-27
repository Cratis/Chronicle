// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypeSchemaCache"/>.
/// </summary>
/// <remarks>
/// Sits on the Orleans serialization hot path, so the blocking event type schema lookup only happens on the
/// first occurrence of each event type and generation per silo. A <see cref="Lazy{T}"/> is cached rather than
/// the resolved text, so a burst of first-time resolutions of the same event type blocks one thread on the
/// storage lookup instead of one per message; a failed lookup is evicted so it is retried rather than cached.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> to resolve event type schemas from.</param>
internal sealed class EventTypeSchemaCache(IStorage storage) : IEventTypeSchemaCache
{
    readonly ConcurrentDictionary<EventTypeSchemaKey, Lazy<string>> _schemaJsonByEventType = new();

    /// <inheritdoc/>
    public string GetSchemaJsonFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation)
    {
        var key = new EventTypeSchemaKey(eventStore, eventTypeId, generation);
        var schemaJson = _schemaJsonByEventType.GetOrAdd(
            key,
            static (keyToResolve, cache) => new Lazy<string>(
                () => cache.ResolveSchemaJson(keyToResolve),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        try
        {
            return schemaJson.Value;
        }
        catch
        {
            _schemaJsonByEventType.TryRemove(new KeyValuePair<EventTypeSchemaKey, Lazy<string>>(key, schemaJson));
            throw;
        }
    }

    string ResolveSchemaJson(EventTypeSchemaKey key)
    {
        var eventStore = storage.GetEventStore(key.EventStore);
        var eventType = eventStore.EventTypes.GetFor(key.EventTypeId, key.Generation).GetAwaiter().GetResult();
        return eventType.Schema.ToJson();
    }

    readonly record struct EventTypeSchemaKey(EventStoreName EventStore, EventTypeId EventTypeId, EventTypeGeneration Generation);
}
