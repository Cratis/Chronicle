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
    readonly ConcurrentDictionary<EventTypeSchemaKey, ProtectionSchema> _protectionSchemas = new();
    readonly object _protectionInvalidation = new();
    long _protectionRevision;

    /// <inheritdoc/>
    public ValueTask<JsonSchema> GetProtectionSchemaFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema ownSchema)
    {
        var key = new EventTypeSchemaKey(eventStore, eventTypeId, generation);
        if (_protectionSchemas.TryGetValue(key, out var cached) && ReferenceEquals(cached.Own, ownSchema))
        {
            return new(cached.Effective);
        }

        return ResolveProtectionSchema(key, ownSchema);
    }

    async ValueTask<JsonSchema> ResolveProtectionSchema(EventTypeSchemaKey key, JsonSchema ownSchema)
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
            var snapshotOwn = definition.Generations.Single(_ => _.Generation == key.Generation).Schema;
            if (ReferenceEquals(effective, snapshotOwn))
            {
                effective = ownSchema;
            }

            lock (_protectionInvalidation)
            {
                // A registration during the load must not republish the old definition.
                if (revision != _protectionRevision)
                {
                    continue;
                }

                _protectionSchemas[key] = new(ownSchema, effective);
                return effective;
            }
        }
    }

    sealed record ProtectionSchema(JsonSchema Own, JsonSchema Effective);
}
