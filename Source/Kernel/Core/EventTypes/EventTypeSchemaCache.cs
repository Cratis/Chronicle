// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypeSchemaCache"/> as a view over the event type registry.
/// </summary>
/// <remarks>
/// Sits on the Orleans serialization hot path. It holds no cache of its own to invalidate: the schemas and definitions
/// it reads are the shared instances of the registry's current snapshot, and what it derives from them - the schema
/// text and the effective protection schema - hangs off those instances through weak tables. A changed event type is
/// a new instance in the next snapshot, so its derived values are computed anew and the old ones go away with the old
/// instance.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> whose event type registry is read.</param>
internal sealed class EventTypeSchemaCache(IStorage storage) : IEventTypeSchemaCache
{
    readonly ConditionalWeakTable<JsonSchema, string> _schemaJson = new();

    /// <inheritdoc/>
    public string GetSchemaJsonFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation)
    {
        var eventType = storage.GetEventStore(eventStore).EventTypes.GetFor(eventTypeId, generation).GetAwaiter().GetResult();
        return _schemaJson.GetValue(eventType.Schema, static schema => schema.ToJson());
    }

    /// <inheritdoc/>
    public async ValueTask<JsonSchema> GetProtectionSchemaFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema ownSchema)
    {
        var definition = await storage.GetEventStore(eventStore).EventTypes.GetDefinition(eventTypeId);
        return GenerationProtectionSchemas.For(definition, generation);
    }
}
