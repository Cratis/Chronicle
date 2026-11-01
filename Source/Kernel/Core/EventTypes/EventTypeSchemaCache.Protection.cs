// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes;

internal sealed partial class EventTypeSchemaCache
{
    readonly ConcurrentDictionary<EventTypeSchemaKey, JsonSchema> _protectionSchemas = new();
    readonly object _protectionInvalidation = new();
    long _protectionRevision;

    /// <inheritdoc/>
    public ValueTask<JsonSchema> GetProtectionSchemaFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema ownSchema)
    {
        // Validity depends only on the key: Invalidate and the revision fence keep entries fresh. Callers may
        // hold different (or older) schema instances for the same generation, so the instance never decides.
        var key = new EventTypeSchemaKey(eventStore, eventTypeId, generation);
        return _protectionSchemas.TryGetValue(key, out var effective)
            ? new(effective)
            : ResolveProtectionSchema(key);
    }

    async ValueTask<JsonSchema> ResolveProtectionSchema(EventTypeSchemaKey key)
    {
        while (true)
        {
            long revision;
            lock (_protectionInvalidation)
            {
                revision = _protectionRevision;
            }

            var definition = await storage.GetEventStore(key.EventStore).EventTypes.GetDefinition(key.EventTypeId);
            var effective = GenerationProtectionSchemas.For(definition, key.Generation);

            lock (_protectionInvalidation)
            {
                // A registration during the load must not republish the old definition.
                if (revision != _protectionRevision)
                {
                    continue;
                }

                _protectionSchemas[key] = effective;
                return effective;
            }
        }
    }
}
